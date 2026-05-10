using DayPlannerApp.Repositories;

namespace DayPlannerApp.Services;

/// <summary>
/// Implementation of IModuleContext providing modules with controlled access to core services
/// </summary>
public class ModuleContext : IModuleContext
{
    public ITaskManager TaskManager { get; }
    public IConfigurationRepository Configuration { get; }
    public ILogger Logger { get; }

    public ModuleContext(
        ITaskManager taskManager,
        IConfigurationRepository configuration,
        ILogger logger)
    {
        TaskManager = taskManager;
        Configuration = configuration;
        Logger = logger;
    }
}
