using System;
using DayPlannerApp.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DayPlannerApp.ViewModels;

public class MainViewModel : ViewModelBase
{
    public TaskListViewModel TaskListViewModel { get; }
    public DayViewViewModel DayViewViewModel { get; }
    public WeekViewViewModel WeekViewViewModel { get; }
    public CoordinateControllerViewModel CoordinateControllerViewModel { get; }
    public SettingsViewModel SettingsViewModel { get; }

    public MainViewModel(
        ITaskManager taskManager,
        ITagManager tagManager,
        ITimeTracker timeTracker,
        ICalendarViewManager calendarViewManager,
        ICoordinateController coordinateController,
        ITaskTypeManager taskTypeManager,
        INotificationService notificationService,
        ILogger logger,
        IServiceProvider serviceProvider)
    {
        // Create all ViewModels upfront
        TaskListViewModel = new TaskListViewModel(taskManager, tagManager, taskTypeManager, serviceProvider);
        DayViewViewModel = new DayViewViewModel(calendarViewManager, timeTracker, taskManager, notificationService, logger);
        WeekViewViewModel = new WeekViewViewModel(calendarViewManager);
        CoordinateControllerViewModel = new CoordinateControllerViewModel(coordinateController, taskManager);
        SettingsViewModel = new SettingsViewModel(taskTypeManager, tagManager, notificationService, logger);
        
        // Load initial data
        _ = InitializeAsync();
    }

    private async System.Threading.Tasks.Task InitializeAsync()
    {
        await TaskListViewModel.LoadTasksAsync();
        await DayViewViewModel.LoadAsync();
        await WeekViewViewModel.LoadAsync();
        await CoordinateControllerViewModel.LoadAsync();
        await SettingsViewModel.LoadAsync();
    }
}
