using System;
using System.Threading;
using System.Threading.Tasks;
using DayPlannerApp.Modules;
using DayPlannerApp.Models;

namespace DayPlannerApp.Examples
{
    /// <summary>
    /// Example module demonstrating Day Planner extensibility.
    /// This module performs periodic task cleanup and logging.
    /// </summary>
    public class ExampleModule : IModule
    {
        public string Id => "example-module";
        public string Name => "Example Module";
        public string Version => "1.0.0";
        
        private IModuleContext _context;
        private Timer _cleanupTimer;
        private const string ConfigKeyEnabled = "ExampleModule.Enabled";
        private const string ConfigKeyIntervalMinutes = "ExampleModule.IntervalMinutes";
        
        public async Task InitializeAsync(IModuleContext context)
        {
            _context = context;
            
            // Check if module is enabled
            var enabled = await _context.Configuration.GetSettingAsync<bool>(ConfigKeyEnabled);
            if (!enabled)
            {
                _context.Logger.LogInformation("ExampleModule is disabled in configuration");
                return;
            }
            
            // Read configuration
            var intervalMinutes = await _context.Configuration
                .GetSettingAsync<int>(ConfigKeyIntervalMinutes) ?? 60;
            
            // Setup periodic cleanup
            _cleanupTimer = new Timer(
                async _ => await PerformCleanupAsync(),
                null,
                TimeSpan.Zero,
                TimeSpan.FromMinutes(intervalMinutes)
            );
            
            _context.Logger.LogInformation(
                $"ExampleModule initialized (interval: {intervalMinutes} minutes)"
            );
        }
        
        private async Task PerformCleanupAsync()
        {
            try
            {
                _context.Logger.LogInformation("Starting periodic cleanup");
                
                // Example: Find old completed tasks
                var thirtyDaysAgo = DateTime.Now.AddDays(-30);
                var oldTasks = await _context.TaskManager.GetTasksByDateRangeAsync(
                    DateTime.MinValue,
                    thirtyDaysAgo
                );
                
                var completedCount = 0;
                foreach (var task in oldTasks)
                {
                    // Example logic: Log old tasks (don't actually delete)
                    if (task.Description.Contains("[COMPLETED]"))
                    {
                        _context.Logger.LogInformation(
                            $"Found old completed task: {task.Id} - {task.Description}"
                        );
                        completedCount++;
                    }
                }
                
                _context.Logger.LogInformation(
                    $"Cleanup complete: found {completedCount} old completed tasks"
                );
            }
            catch (Exception ex)
            {
                _context.Logger.LogError($"Cleanup failed: {ex.Message}");
            }
        }
        
        public async Task ShutdownAsync()
        {
            _cleanupTimer?.Dispose();
            _context.Logger.LogInformation("ExampleModule shutting down");
            await Task.CompletedTask;
        }
    }
}
