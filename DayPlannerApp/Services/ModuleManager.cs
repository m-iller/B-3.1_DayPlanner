using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DayPlannerApp.Models;
using DayPlannerApp.Repositories;

namespace DayPlannerApp.Services;

/// <summary>
/// Manages module discovery, loading, and lifecycle
/// </summary>
public class ModuleManager : IModuleManager
{
    private const string MODULES_DIRECTORY = "Modules";
    
    private readonly IModuleContext _moduleContext;
    private readonly IModuleRepository _moduleRepository;
    private readonly ILogger _logger;
    private readonly Dictionary<string, IModule> _loadedModules;
    private readonly Dictionary<string, ModuleInfo> _moduleInfoCache;

    public ModuleManager(
        ITaskManager taskManager,
        IConfigurationRepository configuration,
        IModuleRepository moduleRepository,
        ILogger logger)
    {
        _moduleContext = new ModuleContext(taskManager, configuration, logger);
        _moduleRepository = moduleRepository ?? throw new ArgumentNullException(nameof(moduleRepository));
        _logger = logger;
        _loadedModules = new Dictionary<string, IModule>();
        _moduleInfoCache = new Dictionary<string, ModuleInfo>();
    }

    public async Task<IEnumerable<ModuleInfo>> GetInstalledModulesAsync()
    {
        // First, get modules from database
        var dbModules = await _moduleRepository.GetAllAsync();
        var dbModuleDict = dbModules.ToDictionary(m => m.Id);
        
        // Discover modules in Modules/ directory
        var modulesPath = GetModulesDirectory();
        if (!Directory.Exists(modulesPath))
        {
            _logger.Info($"Modules directory not found: {modulesPath}");
            return dbModules;
        }

        var dllFiles = Directory.GetFiles(modulesPath, "*.dll", SearchOption.TopDirectoryOnly);
        var discoveredModules = new List<ModuleInfo>();
        
        foreach (var dllPath in dllFiles)
        {
            try
            {
                var moduleInfo = await DiscoverModuleAsync(dllPath);
                if (moduleInfo != null)
                {
                    // Check if module exists in database
                    if (dbModuleDict.TryGetValue(moduleInfo.Id, out var dbModule))
                    {
                        // Update from database state
                        moduleInfo.IsEnabled = dbModule.IsEnabled;
                        moduleInfo.IsLoaded = _loadedModules.ContainsKey(moduleInfo.Id);
                        moduleInfo.LoadedAt = moduleInfo.IsLoaded ? _moduleInfoCache[moduleInfo.Id].LoadedAt : DateTime.MinValue;
                    }
                    else
                    {
                        // New module discovered - add to database
                        moduleInfo.IsEnabled = true;
                        await _moduleRepository.InsertAsync(moduleInfo);
                    }
                    
                    discoveredModules.Add(moduleInfo);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to discover module at {dllPath}", ex);
            }
        }

        return discoveredModules;
    }

    public async Task<ModuleInfo> LoadModuleAsync(string modulePath)
    {
        if (string.IsNullOrWhiteSpace(modulePath))
        {
            throw new ArgumentException("Module path cannot be empty", nameof(modulePath));
        }

        if (!File.Exists(modulePath))
        {
            throw new FileNotFoundException($"Module assembly not found: {modulePath}");
        }

        // Load assembly
        Assembly assembly;
        try
        {
            assembly = Assembly.LoadFrom(modulePath);
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to load assembly from {modulePath}", ex);
            throw new InvalidOperationException($"Failed to load module assembly: {ex.Message}", ex);
        }

        // Find IModule implementation
        var moduleType = FindModuleType(assembly);
        if (moduleType == null)
        {
            throw new InvalidOperationException($"No IModule implementation found in {modulePath}");
        }

        // Create module instance
        IModule module;
        try
        {
            module = (IModule)Activator.CreateInstance(moduleType)!;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to instantiate module type {moduleType.FullName}", ex);
            throw new InvalidOperationException($"Failed to create module instance: {ex.Message}", ex);
        }

        // Check if already loaded (idempotent)
        if (_loadedModules.ContainsKey(module.Id))
        {
            _logger.Warning($"Module {module.Id} is already loaded");
            return _moduleInfoCache[module.Id];
        }

        // Initialize module
        try
        {
            await module.InitializeAsync(_moduleContext);
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to initialize module {module.Id}", ex);
            throw new InvalidOperationException($"Module initialization failed: {ex.Message}", ex);
        }

        // Create module info
        var moduleInfo = new ModuleInfo
        {
            Id = module.Id,
            Name = module.Name,
            Version = module.Version,
            Description = string.Empty,
            AssemblyPath = modulePath,
            IsEnabled = true,
            IsLoaded = true,
            LoadedAt = DateTime.UtcNow
        };

        // Track loaded module in memory
        _loadedModules[module.Id] = module;
        _moduleInfoCache[module.Id] = moduleInfo;

        // Persist to database
        var existingModule = await _moduleRepository.GetByIdAsync(module.Id);
        if (existingModule != null)
        {
            // Update existing record
            existingModule.IsLoaded = true;
            existingModule.LoadedAt = moduleInfo.LoadedAt;
            existingModule.AssemblyPath = modulePath;
            existingModule.Name = module.Name;
            existingModule.Version = module.Version;
            await _moduleRepository.UpdateAsync(existingModule);
        }
        else
        {
            // Insert new record
            await _moduleRepository.InsertAsync(moduleInfo);
        }

        _logger.Info($"Module loaded: {module.Name} (v{module.Version})");

        return moduleInfo;
    }

    public async Task UnloadModuleAsync(string moduleId)
    {
        if (string.IsNullOrWhiteSpace(moduleId))
        {
            throw new ArgumentException("Module ID cannot be empty", nameof(moduleId));
        }

        if (!_loadedModules.TryGetValue(moduleId, out var module))
        {
            throw new InvalidOperationException($"Module {moduleId} is not loaded");
        }

        try
        {
            await module.ShutdownAsync();
        }
        catch (Exception ex)
        {
            _logger.Error($"Error during module shutdown: {moduleId}", ex);
            // Continue with unload even if shutdown fails
        }

        // Remove from memory
        _loadedModules.Remove(moduleId);
        
        if (_moduleInfoCache.TryGetValue(moduleId, out var moduleInfo))
        {
            moduleInfo.IsLoaded = false;
        }

        // Update database
        var dbModule = await _moduleRepository.GetByIdAsync(moduleId);
        if (dbModule != null)
        {
            dbModule.IsLoaded = false;
            dbModule.LoadedAt = DateTime.MinValue;
            await _moduleRepository.UpdateAsync(dbModule);
        }

        _logger.Info($"Module unloaded: {moduleId}");
    }

    public Task<bool> IsModuleLoadedAsync(string moduleId)
    {
        if (string.IsNullOrWhiteSpace(moduleId))
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(_loadedModules.ContainsKey(moduleId));
    }

    private async Task<ModuleInfo?> DiscoverModuleAsync(string assemblyPath)
    {
        try
        {
            var assembly = Assembly.LoadFrom(assemblyPath);
            var moduleType = FindModuleType(assembly);
            
            if (moduleType == null)
            {
                return null;
            }

            // Create temporary instance to read metadata
            var module = (IModule)Activator.CreateInstance(moduleType)!;
            
            var moduleInfo = new ModuleInfo
            {
                Id = module.Id,
                Name = module.Name,
                Version = module.Version,
                Description = string.Empty,
                AssemblyPath = assemblyPath,
                IsLoaded = _loadedModules.ContainsKey(module.Id),
                LoadedAt = _loadedModules.ContainsKey(module.Id) 
                    ? _moduleInfoCache[module.Id].LoadedAt 
                    : DateTime.MinValue
            };

            return moduleInfo;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to discover module at {assemblyPath}", ex);
            return null;
        }
    }

    private Type? FindModuleType(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes()
                .FirstOrDefault(t => typeof(IModule).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
        }
        catch (ReflectionTypeLoadException ex)
        {
            _logger.Error($"Failed to load types from assembly {assembly.FullName}", ex);
            return null;
        }
    }

    private string GetModulesDirectory()
    {
        var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(appDirectory, MODULES_DIRECTORY);
    }
}
