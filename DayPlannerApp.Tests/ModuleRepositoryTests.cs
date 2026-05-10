using System;
using System.Linq;
using System.Threading.Tasks;
using DayPlannerApp.Models;
using DayPlannerApp.Repositories;
using Xunit;

namespace DayPlannerApp.Tests;

public class ModuleRepositoryTests : IDisposable
{
    private readonly TestDatabaseHelper _dbHelper;
    private readonly ModuleRepository _repository;

    public ModuleRepositoryTests()
    {
        _dbHelper = new TestDatabaseHelper();
        _repository = new ModuleRepository(_dbHelper.ConnectionString);
    }

    [Fact]
    public async Task InsertAsync_ValidModule_InsertsSuccessfully()
    {
        // Arrange
        var module = new ModuleInfo
        {
            Id = "test-module-1",
            Name = "Test Module",
            Version = "1.0.0",
            Description = "Test module description",
            AssemblyPath = "/path/to/module.dll",
            IsEnabled = true,
            IsLoaded = false,
            LoadedAt = DateTime.MinValue
        };

        // Act
        var result = await _repository.InsertAsync(module);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(module.Id, result.Id);
        Assert.Equal(module.Name, result.Name);
        Assert.Equal(module.Version, result.Version);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingModule_ReturnsModule()
    {
        // Arrange
        var module = new ModuleInfo
        {
            Id = "test-module-2",
            Name = "Test Module 2",
            Version = "2.0.0",
            Description = "Another test module",
            AssemblyPath = "/path/to/module2.dll",
            IsEnabled = true,
            IsLoaded = false,
            LoadedAt = DateTime.MinValue
        };
        await _repository.InsertAsync(module);

        // Act
        var result = await _repository.GetByIdAsync("test-module-2");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-module-2", result.Id);
        Assert.Equal("Test Module 2", result.Name);
        Assert.Equal("2.0.0", result.Version);
        Assert.Equal("Another test module", result.Description);
        Assert.Equal("/path/to/module2.dll", result.AssemblyPath);
        Assert.True(result.IsEnabled);
        Assert.False(result.IsLoaded);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentModule_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync("non-existent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_ExistingModule_UpdatesSuccessfully()
    {
        // Arrange
        var module = new ModuleInfo
        {
            Id = "test-module-3",
            Name = "Test Module 3",
            Version = "1.0.0",
            Description = "Original description",
            AssemblyPath = "/path/to/module3.dll",
            IsEnabled = true,
            IsLoaded = false,
            LoadedAt = DateTime.MinValue
        };
        await _repository.InsertAsync(module);

        // Act
        module.Name = "Updated Module 3";
        module.Version = "1.1.0";
        module.Description = "Updated description";
        module.IsLoaded = true;
        module.LoadedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(module);

        // Assert
        var result = await _repository.GetByIdAsync("test-module-3");
        Assert.NotNull(result);
        Assert.Equal("Updated Module 3", result.Name);
        Assert.Equal("1.1.0", result.Version);
        Assert.Equal("Updated description", result.Description);
        Assert.True(result.IsLoaded);
        Assert.NotEqual(DateTime.MinValue, result.LoadedAt);
    }

    [Fact]
    public async Task DeleteAsync_ExistingModule_DeletesSuccessfully()
    {
        // Arrange
        var module = new ModuleInfo
        {
            Id = "test-module-4",
            Name = "Test Module 4",
            Version = "1.0.0",
            Description = "To be deleted",
            AssemblyPath = "/path/to/module4.dll",
            IsEnabled = true,
            IsLoaded = false,
            LoadedAt = DateTime.MinValue
        };
        await _repository.InsertAsync(module);

        // Act
        await _repository.DeleteAsync("test-module-4");

        // Assert
        var result = await _repository.GetByIdAsync("test-module-4");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_MultipleModules_ReturnsAllModules()
    {
        // Arrange
        var module1 = new ModuleInfo
        {
            Id = "module-a",
            Name = "Module A",
            Version = "1.0.0",
            Description = "First module",
            AssemblyPath = "/path/to/a.dll",
            IsEnabled = true,
            IsLoaded = false,
            LoadedAt = DateTime.MinValue
        };
        var module2 = new ModuleInfo
        {
            Id = "module-b",
            Name = "Module B",
            Version = "2.0.0",
            Description = "Second module",
            AssemblyPath = "/path/to/b.dll",
            IsEnabled = false,
            IsLoaded = false,
            LoadedAt = DateTime.MinValue
        };
        await _repository.InsertAsync(module1);
        await _repository.InsertAsync(module2);

        // Act
        var results = await _repository.GetAllAsync();

        // Assert
        Assert.Equal(2, results.Count());
        Assert.Contains(results, m => m.Id == "module-a");
        Assert.Contains(results, m => m.Id == "module-b");
    }

    [Fact]
    public async Task GetEnabledAsync_MixedModules_ReturnsOnlyEnabled()
    {
        // Arrange
        var enabledModule = new ModuleInfo
        {
            Id = "enabled-module",
            Name = "Enabled Module",
            Version = "1.0.0",
            Description = "This is enabled",
            AssemblyPath = "/path/to/enabled.dll",
            IsEnabled = true,
            IsLoaded = false,
            LoadedAt = DateTime.MinValue
        };
        var disabledModule = new ModuleInfo
        {
            Id = "disabled-module",
            Name = "Disabled Module",
            Version = "1.0.0",
            Description = "This is disabled",
            AssemblyPath = "/path/to/disabled.dll",
            IsEnabled = false,
            IsLoaded = false,
            LoadedAt = DateTime.MinValue
        };
        await _repository.InsertAsync(enabledModule);
        await _repository.InsertAsync(disabledModule);

        // Act
        var results = await _repository.GetEnabledAsync();

        // Assert
        Assert.Single(results);
        Assert.Equal("enabled-module", results.First().Id);
    }

    [Fact]
    public async Task InsertAsync_LoadedModule_StoresLoadedAtTimestamp()
    {
        // Arrange
        var loadedAt = DateTime.UtcNow;
        var module = new ModuleInfo
        {
            Id = "loaded-module",
            Name = "Loaded Module",
            Version = "1.0.0",
            Description = "Currently loaded",
            AssemblyPath = "/path/to/loaded.dll",
            IsEnabled = true,
            IsLoaded = true,
            LoadedAt = loadedAt
        };

        // Act
        await _repository.InsertAsync(module);
        var result = await _repository.GetByIdAsync("loaded-module");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("loaded-module", result.Id);
        Assert.Equal("Loaded Module", result.Name);
        // LoadedAt timestamp should be preserved
        Assert.NotEqual(DateTime.MinValue, result.LoadedAt);
        // Timestamp should match within 2 second tolerance
        var timeDiff = Math.Abs((result.LoadedAt - loadedAt).TotalSeconds);
        Assert.True(timeDiff < 2, $"Time difference was {timeDiff} seconds, expected < 2");
    }

    public void Dispose()
    {
        _dbHelper?.Dispose();
    }
}
