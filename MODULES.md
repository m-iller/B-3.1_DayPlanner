# Day Planner Module System

Plugin architecture for extending Day Planner functionality.

## Overview

Modules are .NET assemblies that implement `IModule` interface. They run in the same process as Day Planner and access core services via `IModuleContext`.

## Creating a Module

### 1. Create Class Library Project

```bash
dotnet new classlib -n MyDayPlannerModule
cd MyDayPlannerModule
dotnet add reference ../DayPlannerApp/DayPlannerApp.csproj
```

### 2. Implement IModule

```csharp
using DayPlannerApp.Modules;

public class MyModule : IModule
{
    public string Id => "my-module";
    public string Name => "My Module";
    public string Version => "1.0.0";
    
    private IModuleContext _context;
    
    public async Task InitializeAsync(IModuleContext context)
    {
        _context = context;
        
        // Setup logic here
        _context.Logger.LogInformation("MyModule initialized");
        
        // Access services
        var tasks = await _context.TaskManager.GetTasksByDateRangeAsync(
            DateTime.Today, 
            DateTime.Today.AddDays(7)
        );
        
        // Do something with tasks
    }
    
    public async Task ShutdownAsync()
    {
        // Cleanup logic here
        _context.Logger.LogInformation("MyModule shutting down");
    }
}
```

### 3. Build and Deploy

```bash
dotnet build -c Release
copy bin/Release/net8.0/MyDayPlannerModule.dll ../DayPlannerApp/Modules/
```

## IModuleContext API

Modules access Day Planner services via context:

### TaskManager

```csharp
// Create task
var task = await context.TaskManager.CreateTaskAsync(new TaskCreateDto
{
    Description = "Task from module",
    TaskTypeId = 1,
    UrgencyLevel = 5
});

// Query tasks
var tasks = await context.TaskManager.GetTasksByTagsAsync(new[] { "important" });

// Update task
await context.TaskManager.UpdateTaskAsync(taskId, new TaskUpdateDto
{
    Description = "Updated description"
});

// Delete task
await context.TaskManager.DeleteTaskAsync(taskId);
```

### Configuration

```csharp
// Read setting
var apiKey = await context.Configuration.GetSettingAsync<string>("MyModule.ApiKey");

// Write setting
await context.Configuration.SetSettingAsync("MyModule.ApiKey", "secret123");
```

### Logger

```csharp
context.Logger.LogInformation("Info message");
context.Logger.LogWarning("Warning message");
context.Logger.LogError("Error message");
```

## Module Lifecycle

1. **Discovery**: Day Planner scans `Modules/` directory on startup
2. **Load**: Assembly loaded via `Assembly.LoadFrom`
3. **Initialize**: `InitializeAsync` called with context
4. **Running**: Module can respond to events, schedule work
5. **Shutdown**: `ShutdownAsync` called on app exit
6. **Unload**: (Optional) Module can be unloaded at runtime

## Example: Calendar Sync Module

```csharp
public class CalendarSyncModule : IModule
{
    public string Id => "calendar-sync";
    public string Name => "Calendar Sync";
    public string Version => "1.0.0";
    
    private IModuleContext _context;
    private Timer _syncTimer;
    
    public async Task InitializeAsync(IModuleContext context)
    {
        _context = context;
        
        // Read config
        var syncInterval = await context.Configuration
            .GetSettingAsync<int>("CalendarSync.IntervalMinutes") ?? 30;
        
        // Setup periodic sync
        _syncTimer = new Timer(
            async _ => await SyncTasksAsync(),
            null,
            TimeSpan.Zero,
            TimeSpan.FromMinutes(syncInterval)
        );
        
        context.Logger.LogInformation($"Calendar sync started (interval: {syncInterval}m)");
    }
    
    private async Task SyncTasksAsync()
    {
        try
        {
            // Get tasks from Day Planner
            var tasks = await _context.TaskManager.GetTasksByDateRangeAsync(
                DateTime.Today,
                DateTime.Today.AddDays(30)
            );
            
            // Sync to external calendar API
            foreach (var task in tasks)
            {
                if (task.DeadlineDate.HasValue)
                {
                    await SyncToExternalCalendar(task);
                }
            }
            
            _context.Logger.LogInformation($"Synced {tasks.Count()} tasks");
        }
        catch (Exception ex)
        {
            _context.Logger.LogError($"Sync failed: {ex.Message}");
        }
    }
    
    private async Task SyncToExternalCalendar(TaskEntity task)
    {
        // External API call here
        await Task.CompletedTask;
    }
    
    public async Task ShutdownAsync()
    {
        _syncTimer?.Dispose();
        _context.Logger.LogInformation("Calendar sync stopped");
        await Task.CompletedTask;
    }
}
```

## Security Considerations

### What Modules CAN Do

- Access all tasks via TaskManager
- Read/write configuration settings (namespaced recommended)
- Log messages
- Make external API calls
- Schedule background work

### What Modules CANNOT Do

- Direct database access (must use TaskManager)
- Access other modules directly
- Modify core application behavior
- Access file system outside their own directory (not enforced, but discouraged)

### Best Practices

- Namespace configuration keys: `ModuleName.SettingKey`
- Handle errors gracefully (don't crash host app)
- Log important operations
- Validate external data
- Use async/await properly
- Dispose resources in ShutdownAsync

## Module Configuration

Store module settings in Day Planner configuration:

```csharp
// In module initialization
var apiKey = await context.Configuration.GetSettingAsync<string>("MyModule.ApiKey");
if (string.IsNullOrEmpty(apiKey))
{
    context.Logger.LogWarning("MyModule.ApiKey not configured");
    // Prompt user or use default
}
```

Users can set values via:
- Settings UI (if implemented)
- Direct database edit
- IPC API

## Debugging Modules

### Attach Debugger

1. Build module in Debug configuration
2. Copy to `Modules/` directory
3. Start Day Planner
4. Attach Visual Studio debugger to DayPlannerApp.exe process
5. Set breakpoints in module code

### Logging

Use `context.Logger` extensively:

```csharp
context.Logger.LogDebug($"Processing task {task.Id}");
context.Logger.LogInformation($"Sync completed: {count} tasks");
context.Logger.LogWarning($"API rate limit approaching");
context.Logger.LogError($"Failed to sync: {ex.Message}");
```

## Distribution

### Package Structure

```
MyModule/
├── MyModule.dll
├── MyModule.pdb (optional, for debugging)
├── README.md
└── dependencies/ (if any)
```

### Installation Instructions

1. Download module package
2. Extract to `DayPlannerApp/Modules/MyModule/`
3. Restart Day Planner
4. Verify in Settings → Modules

## Limitations

- Modules run in same process (crash can affect host)
- No sandboxing or permission system
- No versioning/dependency management
- Manual installation only

## Future Enhancements

(Not yet implemented)

- Event system for task changes
- UI extension points
- Module marketplace
- Automatic updates
- Permission system
- Module-to-module communication

## Example Modules

### Backup Module

Periodic database backup to cloud storage.

### Pomodoro Timer

Integrate pomodoro technique with time tracking.

### GitHub Integration

Create tasks from GitHub issues.

### Email Parser

Create tasks from email subjects.

## Support

For module development questions, see main documentation or contact support.
