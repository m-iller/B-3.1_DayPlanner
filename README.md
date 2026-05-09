# Day Planner App

Windows desktop task management application with calendar views, time tracking, and flexible organization.

## Features

### Task Management
- Create, edit, and delete tasks with markdown formatting
- Four configurable task types (personal projects, learning, work, must do)
- Custom tags for flexible categorization
- Urgency levels and deadline tracking
- Coordinate-based prioritization (importance × complexity axes)

### Calendar Views
- **Day View**: Focus on single-day planning
- **Week View**: Seven-day overview with task summaries
- Date navigation and task filtering

### Time Tracking
- Start/stop tracking with break support
- Automatic duration calculation (excluding breaks)
- Historical session data per task

### Extensibility
- **Module System**: Plugin architecture for external integrations
- **IPC API**: Named pipe interface for automation and external tools

## Architecture

Built with WPF (.NET 8) using MVVM pattern:

```
UI Layer (XAML/ViewModels)
    ↓
Business Logic Layer (Services)
    ↓
Data Layer (Repositories)
    ↓
SQLite Database
```

### Key Components

- **TaskManager**: Task CRUD and filtering
- **TimeTracker**: Session tracking with Stopwatch
- **MarkdownProcessor**: Markdig-based rendering
- **CalendarViewManager**: Date-based task aggregation
- **TagManager**: Tag operations and search
- **CoordinateController**: Importance/complexity positioning
- **ModuleManager**: Plugin lifecycle management
- **IPCServer**: Inter-process communication

## Technology Stack

- **Framework**: WPF (.NET 8)
- **Database**: SQLite (Microsoft.Data.Sqlite)
- **Markdown**: Markdig
- **Testing**: xUnit + FsCheck (property-based testing)
- **DI**: Microsoft.Extensions.DependencyInjection

## Data Storage

All data stored locally in SQLite database:
- Tasks with markdown descriptions
- Time tracking sessions with break periods
- Tags and task-tag associations
- Configuration settings
- Module metadata
- IPC authentication tokens

Database location: `%LOCALAPPDATA%/DayPlannerApp/dayplanner.db`

## Module System

Extend functionality via plugins:

```csharp
public interface IModule
{
    string Id { get; }
    string Name { get; }
    string Version { get; }
    Task InitializeAsync(IModuleContext context);
    Task ShutdownAsync();
}
```

Modules access core services via `IModuleContext`:
- TaskManager
- Configuration
- Logger

Place modules in: `Modules/` directory

See [MODULES.md](MODULES.md) for details.

## IPC API

External applications can integrate via named pipes:

**Pipe Name**: `DayPlannerApp.IPC`

**Protocol**: JSON request/response

**Authentication**: Shared secret token

**Supported Operations**:
- Task CRUD
- Time tracking control
- Query by filters
- Data export

See [IPC.md](IPC.md) for API reference.

## Building

```bash
dotnet build DayPlannerApp/DayPlannerApp.csproj
```

## Running

```bash
dotnet run --project DayPlannerApp/DayPlannerApp.csproj
```

## Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true
```

## Project Structure

```
DayPlannerApp/
├── Views/              # XAML UI components
├── ViewModels/         # MVVM view models
├── Models/             # Domain entities
├── Services/           # Business logic
├── Repositories/       # Data access
├── Data/               # Database initialization
└── Modules/            # Plugin directory
```

## Design Principles

- **Offline-first**: No network dependency for core features
- **Single-user**: Local data storage
- **MVVM**: Clear separation of concerns
- **Property-based testing**: Universal correctness properties
- **Extensibility**: Module system for integrations

## Requirements

- Windows 10/11
- .NET 8 Runtime

## License

[Your License Here]
