using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using DayPlannerApp.Models;
using DayPlannerApp.Services;

namespace DayPlannerApp.ViewModels;

public class DayViewViewModel : ViewModelBase
{
    private readonly ICalendarViewManager _calendarViewManager;
    private readonly ITimeTracker _timeTracker;
    private readonly ITaskManager _taskManager;
    private readonly INotificationService _notificationService;
    private readonly ILogger _logger;
    private readonly DispatcherTimer _uiUpdateTimer;
    private int _tickCount;

    private DateTime _selectedDate = DateTime.Today;
    private ObservableCollection<TaskTimeTrackingWrapper> _tasksForDay = new();
    private TimeSpan _totalTimeWorkedToday;
    private int _activeTasksCount;
    private int _completedTasksCount;
    private ObservableCollection<TaskTimeStatistic> _taskTimeBreakdown = new();

    public DayViewViewModel(
        ICalendarViewManager calendarViewManager,
        ITimeTracker timeTracker,
        ITaskManager taskManager,
        INotificationService notificationService,
        ILogger logger)
    {
        _calendarViewManager = calendarViewManager;
        _timeTracker = timeTracker;
        _taskManager = taskManager;
        _notificationService = notificationService;
        _logger = logger;

        LoadDayCommand = new RelayCommand(async () => await LoadDayAsync());
        NavigateToPreviousDayCommand = new RelayCommand(NavigateToPreviousDay);
        NavigateToNextDayCommand = new RelayCommand(NavigateToNextDay);
        NavigateToTodayCommand = new RelayCommand(NavigateToToday);

        _uiUpdateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _uiUpdateTimer.Tick += OnTimerTick;
    }

    public DateTime SelectedDate
    {
        get => _selectedDate;
        set
        {
            if (SetProperty(ref _selectedDate, value))
            {
                _ = LoadDayAsync();
            }
        }
    }

    public ObservableCollection<TaskTimeTrackingWrapper> TasksForDay
    {
        get => _tasksForDay;
        set => SetProperty(ref _tasksForDay, value);
    }

    public TimeSpan TotalTimeWorkedToday
    {
        get => _totalTimeWorkedToday;
        set => SetProperty(ref _totalTimeWorkedToday, value);
    }

    public int ActiveTasksCount
    {
        get => _activeTasksCount;
        set => SetProperty(ref _activeTasksCount, value);
    }

    public int CompletedTasksCount
    {
        get => _completedTasksCount;
        set => SetProperty(ref _completedTasksCount, value);
    }

    public ObservableCollection<TaskTimeStatistic> TaskTimeBreakdown
    {
        get => _taskTimeBreakdown;
        set => SetProperty(ref _taskTimeBreakdown, value);
    }

    public ICommand LoadDayCommand { get; }
    public ICommand NavigateToPreviousDayCommand { get; }
    public ICommand NavigateToNextDayCommand { get; }
    public ICommand NavigateToTodayCommand { get; }

    public async Task LoadAsync()
    {
        await LoadDayAsync();
        _uiUpdateTimer.Start();
    }

    public async Task StartTrackingAsync(Guid taskId)
    {
        try
        {
            await _timeTracker.StartTrackingAsync(taskId);
            var wrapper = TasksForDay.FirstOrDefault(w => w.TaskId == taskId);
            if (wrapper != null)
            {
                wrapper.IsTracking = true;
                wrapper.IsOnBreak = false;
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to start tracking for task {taskId}", ex);
            _notificationService.ShowError($"Failed to start tracking: {ex.Message}");
        }
    }

    public async Task PauseTrackingAsync(Guid taskId)
    {
        try
        {
            await _timeTracker.StartBreakAsync(taskId);
            var wrapper = TasksForDay.FirstOrDefault(w => w.TaskId == taskId);
            if (wrapper != null)
            {
                wrapper.IsOnBreak = true;
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to pause tracking for task {taskId}", ex);
            _notificationService.ShowError($"Failed to pause tracking: {ex.Message}");
        }
    }

    public async Task ResumeTrackingAsync(Guid taskId)
    {
        try
        {
            await _timeTracker.EndBreakAsync(taskId);
            var wrapper = TasksForDay.FirstOrDefault(w => w.TaskId == taskId);
            if (wrapper != null)
            {
                wrapper.IsOnBreak = false;
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to resume tracking for task {taskId}", ex);
            _notificationService.ShowError($"Failed to resume tracking: {ex.Message}");
        }
    }

    public async Task CompleteTrackingAsync(Guid taskId)
    {
        try
        {
            var wrapper = TasksForDay.FirstOrDefault(w => w.TaskId == taskId);
            if (wrapper != null && wrapper.IsOnBreak)
            {
                await _timeTracker.EndBreakAsync(taskId);
            }

            await _timeTracker.StopTrackingAsync(taskId);
            
            if (wrapper != null)
            {
                wrapper.IsTracking = false;
                wrapper.IsOnBreak = false;
                wrapper.ElapsedTime = TimeSpan.Zero;
                wrapper.BreakTime = TimeSpan.Zero;
                
                // Mark task as completed
                wrapper.IsCompleted = true;
                await _taskManager.UpdateTaskAsync(wrapper.Task);
                
                // Re-sort tasks to move completed to bottom
                SortTasks();
            }

            await RefreshDailyStatsAsync();
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to complete tracking for task {taskId}", ex);
            _notificationService.ShowError($"Failed to complete tracking: {ex.Message}");
        }
    }

    public async Task MarkTaskCompleteAsync(Guid taskId)
    {
        try
        {
            var wrapper = TasksForDay.FirstOrDefault(w => w.TaskId == taskId);
            if (wrapper != null)
            {
                wrapper.IsCompleted = true;
                await _taskManager.UpdateTaskAsync(wrapper.Task);
                
                // Re-sort tasks to move completed to bottom
                SortTasks();
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to mark task {taskId} as complete", ex);
            _notificationService.ShowError($"Failed to mark task as complete: {ex.Message}");
        }
    }

    public async Task MarkTaskIncompleteAsync(Guid taskId)
    {
        try
        {
            var wrapper = TasksForDay.FirstOrDefault(w => w.TaskId == taskId);
            if (wrapper != null)
            {
                wrapper.IsCompleted = false;
                await _taskManager.UpdateTaskAsync(wrapper.Task);
                
                // Re-sort tasks to move back to active section
                SortTasks();
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to mark task {taskId} as incomplete", ex);
            _notificationService.ShowError($"Failed to mark task as incomplete: {ex.Message}");
        }
    }

    private void SortTasks()
    {
        var sorted = TasksForDay
            .OrderBy(w => w.IsCompleted)  // Incomplete tasks first
            .ThenByDescending(w => w.Task.UrgencyLevel)  // Then by urgency
            .ToList();

        TasksForDay.Clear();
        foreach (var task in sorted)
        {
            TasksForDay.Add(task);
        }
    }

    public async Task RefreshDailyStatsAsync()
    {
        try
        {
            var taskStats = new List<TaskTimeStatistic>();
            var totalDuration = TimeSpan.Zero;

            foreach (var wrapper in TasksForDay)
            {
                var historicalDuration = await _timeTracker.GetTotalTaskDurationAsync(wrapper.TaskId);
                var currentDuration = _timeTracker.GetElapsedTime(wrapper.TaskId);
                var totalTaskDuration = historicalDuration + currentDuration;

                if (totalTaskDuration > TimeSpan.Zero)
                {
                    taskStats.Add(new TaskTimeStatistic
                    {
                        TaskId = wrapper.TaskId,
                        TaskDescription = wrapper.Task.Description,
                        TotalDuration = totalTaskDuration
                    });

                    totalDuration += totalTaskDuration;
                }
            }

            foreach (var stat in taskStats)
            {
                stat.PercentageOfDay = totalDuration.TotalSeconds > 0
                    ? (stat.TotalDuration.TotalSeconds / totalDuration.TotalSeconds) * 100
                    : 0;
            }

            taskStats = taskStats.OrderByDescending(s => s.TotalDuration).ToList();

            TotalTimeWorkedToday = totalDuration;
            TaskTimeBreakdown = new ObservableCollection<TaskTimeStatistic>(taskStats);
            ActiveTasksCount = TasksForDay.Count(w => w.IsTracking);
            CompletedTasksCount = taskStats.Count;
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to refresh daily stats", ex);
        }
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        try
        {
            foreach (var wrapper in TasksForDay)
            {
                if (wrapper.IsTracking)
                {
                    wrapper.ElapsedTime = _timeTracker.GetElapsedTime(wrapper.TaskId);

                    if (wrapper.IsOnBreak)
                    {
                        wrapper.BreakTime = _timeTracker.GetBreakTime(wrapper.TaskId);
                    }
                }
            }

            if (_tickCount % 10 == 0)
            {
                _ = RefreshDailyStatsAsync();
            }

            _tickCount++;
        }
        catch (Exception ex)
        {
            _logger.Error("Timer tick failed", ex);
        }
    }

    private async Task LoadDayAsync()
    {
        var tasks = await _calendarViewManager.GetDayViewAsync(SelectedDate);
        var wrappers = new List<TaskTimeTrackingWrapper>();

        foreach (var task in tasks)
        {
            var wrapper = new TaskTimeTrackingWrapper(task, this);
            var currentSession = _timeTracker.GetCurrentSession(task.Id);

            if (currentSession != null)
            {
                wrapper.IsTracking = true;
                wrapper.IsOnBreak = currentSession.Breaks.Any(b => b.EndTime == null);
            }

            wrappers.Add(wrapper);
        }

        // Sort: incomplete tasks first, then by urgency, completed tasks at bottom
        var sorted = wrappers
            .OrderBy(w => w.IsCompleted)
            .ThenByDescending(w => w.Task.UrgencyLevel)
            .ToList();

        TasksForDay = new ObservableCollection<TaskTimeTrackingWrapper>(sorted);
        await RefreshDailyStatsAsync();
    }

    private void NavigateToPreviousDay()
    {
        SelectedDate = SelectedDate.AddDays(-1);
    }

    private void NavigateToNextDay()
    {
        SelectedDate = SelectedDate.AddDays(1);
    }

    private void NavigateToToday()
    {
        SelectedDate = DateTime.Today;
    }
}
