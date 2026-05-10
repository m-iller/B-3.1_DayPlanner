# Example Module

This is a simple example module demonstrating the Day Planner module system.

## Building

```bash
dotnet build ExampleModule/ExampleModule.csproj
```

## Installation

1. Build the module
2. Copy `ExampleModule.dll` to `DayPlannerApp/bin/Debug/net8.0-windows/Modules/`
3. Run Day Planner - the module will be discovered and loaded automatically

## What it does

- Logs "Hello World" message on initialization
- Demonstrates IModule interface implementation
- Shows how to access IModuleContext (Logger, TaskManager, Configuration)

## Creating your own module

1. Create new .NET class library project
2. Reference DayPlannerApp project
3. Implement `IModule` interface
4. Build and copy DLL to Modules folder

### IModule Interface

```csharp
public interface IModule
{
    string Id { get; }           // Unique identifier
    string Name { get; }         // Display name
    string Version { get; }      // Version string
    
    Task InitializeAsync(IModuleContext context);
    Task ShutdownAsync();
}
```

### IModuleContext

Provides access to:
- `ITaskManager` - Create, update, delete, query tasks
- `IConfigurationRepository` - Read/write settings
- `ILogger` - Logging functionality

## Example: Creating tasks from module

```csharp
public async Task InitializeAsync(IModuleContext context)
{
    var task = new TaskEntity
    {
        Id = Guid.NewGuid(),
        Description = "Task created by module",
        TaskTypeId = 1,
        UrgencyLevel = 5,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
    
    await context.TaskManager.CreateTaskAsync(task);
    context.Logger.Info("Created task from module");
}
```
