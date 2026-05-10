using System.Threading.Tasks;

namespace DayPlannerApp.Services;

/// <summary>
/// Interface that all plugin modules must implement
/// </summary>
public interface IModule
{
    /// <summary>
    /// Unique identifier for the module
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Human-readable module name
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Module version string (e.g., "1.0.0")
    /// </summary>
    string Version { get; }
    
    /// <summary>
    /// Initialize the module with access to core services
    /// </summary>
    /// <param name="context">Module execution context providing access to core services</param>
    Task InitializeAsync(IModuleContext context);
    
    /// <summary>
    /// Cleanup and shutdown the module
    /// </summary>
    Task ShutdownAsync();
}
