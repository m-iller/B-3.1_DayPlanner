using System.Windows.Input;
using DayPlannerApp.Services;

namespace DayPlannerApp.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly ITaskManager _taskManager;
    private readonly ITagManager _tagManager;
    private readonly ITimeTracker _timeTracker;
    private readonly ICalendarViewManager _calendarViewManager;
    private readonly ICoordinateController _coordinateController;

    private ViewModelBase? _currentView;

    public MainViewModel(
        ITaskManager taskManager,
        ITagManager tagManager,
        ITimeTracker timeTracker,
        ICalendarViewManager calendarViewManager,
        ICoordinateController coordinateController)
    {
        _taskManager = taskManager;
        _tagManager = tagManager;
        _timeTracker = timeTracker;
        _calendarViewManager = calendarViewManager;
        _coordinateController = coordinateController;

        NavigateToTaskListCommand = new RelayCommand(NavigateToTaskList);
        NavigateToDayViewCommand = new RelayCommand(NavigateToDayView);
        NavigateToWeekViewCommand = new RelayCommand(NavigateToWeekView);
        NavigateToCoordinateControllerCommand = new RelayCommand(NavigateToCoordinateController);
    }

    public ViewModelBase? CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }

    public ICommand NavigateToTaskListCommand { get; }
    public ICommand NavigateToDayViewCommand { get; }
    public ICommand NavigateToWeekViewCommand { get; }
    public ICommand NavigateToCoordinateControllerCommand { get; }

    private void NavigateToTaskList()
    {
        CurrentView = new TaskListViewModel(_taskManager, _tagManager);
    }

    private void NavigateToDayView()
    {
        CurrentView = new DayViewViewModel(_calendarViewManager);
    }

    private void NavigateToWeekView()
    {
        CurrentView = new WeekViewViewModel(_calendarViewManager);
    }

    private void NavigateToCoordinateController()
    {
        CurrentView = new CoordinateControllerViewModel(_coordinateController, _taskManager);
    }
}
