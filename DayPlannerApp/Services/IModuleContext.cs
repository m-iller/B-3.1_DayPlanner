using DayPlannerApp.Repositories;

namespace DayPlannerApp.Services;

/// <summary>
/// Provides modules with controlled access to core application services
/// </summary>
public interface IModuleContext
{
    /// <summary>
    /// Task management service for creating, updating, and querying tasks
    /// </summary>
    ITaskManager TaskManager { get; }
    
    /// <summary>
    /// Configuration repository for reading and writing settings
    /// </summary>
    IConfigurationRepository Configuration { get; }
    
    /// <summary>
    /// Application logger for module diagnostics
    /// </summary>
    ILogger Logger { get; }
}
