using System.Collections.Generic;
using System.Threading.Tasks;
using DayPlannerApp.Models;

namespace DayPlannerApp.Services;

/// <summary>
/// Manages module discovery, loading, and lifecycle
/// </summary>
public interface IModuleManager
{
    /// <summary>
    /// Get all installed modules (loaded and unloaded)
    /// </summary>
    Task<IEnumerable<ModuleInfo>> GetInstalledModulesAsync();
    
    /// <summary>
    /// Load a module from the specified assembly path
    /// </summary>
    /// <param name="modulePath">Path to the module assembly file</param>
    /// <returns>Module metadata</returns>
    Task<ModuleInfo> LoadModuleAsync(string modulePath);
    
    /// <summary>
    /// Unload a previously loaded module
    /// </summary>
    /// <param name="moduleId">Unique module identifier</param>
    Task UnloadModuleAsync(string moduleId);
    
    /// <summary>
    /// Check if a module is currently loaded
    /// </summary>
    /// <param name="moduleId">Unique module identifier</param>
    Task<bool> IsModuleLoadedAsync(string moduleId);
}
