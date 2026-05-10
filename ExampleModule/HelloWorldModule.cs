using System;
using System.Threading.Tasks;
using DayPlannerApp.Services;

namespace ExampleModule;

/// <summary>
/// Example module that demonstrates the module system.
/// This module logs a hello message on initialization.
/// </summary>
public class HelloWorldModule : IModule
{
    public string Id => "hello-world-module";
    public string Name => "Hello World Module";
    public string Version => "1.0.0";

    private IModuleContext? _context;

    public Task InitializeAsync(IModuleContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        
        _context.Logger.Info($"{Name} v{Version} initialized successfully!");
        _context.Logger.Info("This is an example module demonstrating the plugin system.");
        
        return Task.CompletedTask;
    }

    public Task ShutdownAsync()
    {
        _context?.Logger.Info($"{Name} shutting down.");
        return Task.CompletedTask;
    }
}
