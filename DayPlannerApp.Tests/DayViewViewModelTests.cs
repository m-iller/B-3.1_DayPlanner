using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DayPlannerApp.Models;
using DayPlannerApp.Services;
using DayPlannerApp.ViewModels;

namespace DayPlannerApp.Tests;

public class DayViewViewModelTests
{
    private class MockCalendarViewManager : ICalendarViewManager
    {
        public List<TaskEntity> TasksToReturn { get; set; } = new();

        public Task<IEnumerable<TaskEntity>> GetDayViewAsync(DateTime date) => 
            Task.FromResult<IEnumerable<TaskEntity>>(TasksToReturn);
        
        public Task<IEnumerable<TaskEntity>> GetWeekViewAsync(DateTime startDate) => 
            Task.FromResult<IEnumerable<TaskEntity>>(new List<TaskEntity>());

        public Task<IEnumerable<TaskEntity>> GetTasksForDateAsync(DateTime date) => 
            Task.FromResult<IEnumerable<TaskEntity>>(new List<TaskEntity>());
    }

    private class MockTimeTracker : ITimeTracker
    {
        public Dictionary<Guid, bool> TrackingState { get; } = new();
        public Dictionary<Guid, TimeSpan> ElapsedTimes { get; } = new();
        public Dictionary<Guid, TimeSpan> BreakTimes { get; } = new();
        public Dictionary<Guid, TimeTrackingSession?> CurrentSessions { get; } = new();
        public Dictionary<Guid, TimeSpan> TotalDurations { get; } = new();

        public Task StartTrackingAsync(Guid taskId)
        {
            TrackingState[taskId] = true;
            return Task.CompletedTask;
        }

        public Task StopTrackingAsync(Guid taskId)
        {
            TrackingState[taskId] = false;
            ElapsedTimes[taskId] = TimeSpan.Zero;
            return Task.CompletedTask;
        }

        public Task StartBreakAsync(Guid taskId) => Task.CompletedTask;
        public Task EndBreakAsync(Guid taskId) => Task.CompletedTask;

        public TimeSpan GetElapsedTime(Guid taskId) => 
            ElapsedTimes.ContainsKey(taskId) ? ElapsedTimes[taskId] : TimeSpan.Zero;

        public TimeSpan GetBreakTime(Guid taskId) => 
            BreakTimes.ContainsKey(taskId) ? BreakTimes[taskId] : TimeSpan.Zero;

        public TimeTrackingSession? GetCurrentSession(Guid taskId) => 
            CurrentSessions.ContainsKey(taskId) ? CurrentSessions[taskId] : null;

        public Task<TimeSpan> GetTotalTaskDurationAsync(Guid taskId) => 
            Task.FromResult(TotalDurations.ContainsKey(taskId) ? TotalDurations[taskId] : TimeSpan.Zero);

        public Task<TimeSpan> GetTotalBreakTimeAsync(Guid taskId) => 
            Task.FromResult(TimeSpan.Zero);

        public Task StopAllActiveSessionsAsync() => Task.CompletedTask;
    }

    private class MockNotificationService : INotificationService
    {
        public List<string> Errors { get; } = new();
        public void ShowError(string message, string title = "Error") => Errors.Add(message);
        public void ShowWarning(string message, string title = "Warning") { }
        public void ShowInfo(string message, string title = "Information") { }
        public bool ShowConfirmation(string message, string title = "Confirm") => true;
    }

    private class MockLogger : ILogger
    {
        public List<string> ErrorMessages { get; } = new();
        public void Debug(string message) { }
        public void Info(string message) { }
        public void Warning(string message) { }
        public void Error(string message, Exception? exception = null) => ErrorMessages.Add(message);
        public void Fatal(string message, Exception? exception = null) { }
    }

    private class MockTaskManager : ITaskManager
    {
        public Task<TaskEntity> CreateTaskAsync(TaskEntity task) => Task.FromResult(task);
        public Task<TaskEntity> UpdateTaskAsync(TaskEntity task) => Task.FromResult(task);
        public Task DeleteTaskAsync(Guid taskId) => Task.CompletedTask;
        public Task<TaskEntity?> GetTaskByIdAsync(Guid taskId) => Task.FromResult<TaskEntity?>(null);
        public Task<IEnumerable<TaskEntity>> GetTasksByDateRangeAsync(DateTime start, DateTime end) => 
            Task.FromResult<IEnumerable<TaskEntity>>(new List<TaskEntity>());
        public Task<IEnumerable<TaskEntity>> GetTasksByTagsAsync(IEnumerable<string> tags) => 
            Task.FromResult<IEnumerable<TaskEntity>>(new List<TaskEntity>());
        public Task<IEnumerable<TaskEntity>> GetTasksByTypeAsync(int taskTypeId) => 
            Task.FromResult<IEnumerable<TaskEntity>>(new List<TaskEntity>());
        public Task<IEnumerable<TaskEntity>> QueryTasksAsync(TaskQuerySpec spec) => 
            Task.FromResult<IEnumerable<TaskEntity>>(new List<TaskEntity>());
    }

    [Fact]
    public async Task StartTrackingAsync_UpdatesWrapperState()
    {
        var calendarManager = new MockCalendarViewManager();
        var timeTracker = new MockTimeTracker();
        var taskManager = new MockTaskManager();
        var notificationService = new MockNotificationService();
        var logger = new MockLogger();

        var task = new TaskEntity { Id = Guid.NewGuid(), Description = "Test Task" };
        calendarManager.TasksToReturn.Add(task);

        var viewModel = new DayViewViewModel(calendarManager, timeTracker, taskManager, notificationService, logger);
        await viewModel.LoadAsync();

        await viewModel.StartTrackingAsync(task.Id);

        var wrapper = viewModel.TasksForDay.First();
        Assert.True(wrapper.IsTracking);
        Assert.False(wrapper.IsOnBreak);
    }

    [Fact]
    public async Task PauseTrackingAsync_UpdatesWrapperState()
    {
        var calendarManager = new MockCalendarViewManager();
        var timeTracker = new MockTimeTracker();
        var taskManager = new MockTaskManager();
        var notificationService = new MockNotificationService();
        var logger = new MockLogger();

        var task = new TaskEntity { Id = Guid.NewGuid(), Description = "Test Task" };
        calendarManager.TasksToReturn.Add(task);

        var viewModel = new DayViewViewModel(calendarManager, timeTracker, taskManager, notificationService, logger);
        await viewModel.LoadAsync();

        await viewModel.StartTrackingAsync(task.Id);
        await viewModel.PauseTrackingAsync(task.Id);

        var wrapper = viewModel.TasksForDay.First();
        Assert.True(wrapper.IsOnBreak);
    }

    [Fact]
    public async Task ResumeTrackingAsync_UpdatesWrapperState()
    {
        var calendarManager = new MockCalendarViewManager();
        var timeTracker = new MockTimeTracker();
        var taskManager = new MockTaskManager();
        var notificationService = new MockNotificationService();
        var logger = new MockLogger();

        var task = new TaskEntity { Id = Guid.NewGuid(), Description = "Test Task" };
        calendarManager.TasksToReturn.Add(task);

        var viewModel = new DayViewViewModel(calendarManager, timeTracker, taskManager, notificationService, logger);
        await viewModel.LoadAsync();

        await viewModel.StartTrackingAsync(task.Id);
        await viewModel.PauseTrackingAsync(task.Id);
        await viewModel.ResumeTrackingAsync(task.Id);

        var wrapper = viewModel.TasksForDay.First();
        Assert.False(wrapper.IsOnBreak);
    }

    [Fact]
    public async Task CompleteTrackingAsync_EndsBreakIfNeeded()
    {
        var calendarManager = new MockCalendarViewManager();
        var timeTracker = new MockTimeTracker();
        var taskManager = new MockTaskManager();
        var notificationService = new MockNotificationService();
        var logger = new MockLogger();

        var task = new TaskEntity { Id = Guid.NewGuid(), Description = "Test Task" };
        calendarManager.TasksToReturn.Add(task);

        var viewModel = new DayViewViewModel(calendarManager, timeTracker, taskManager, notificationService, logger);
        await viewModel.LoadAsync();

        await viewModel.StartTrackingAsync(task.Id);
        await viewModel.PauseTrackingAsync(task.Id);
        await viewModel.CompleteTrackingAsync(task.Id);

        var wrapper = viewModel.TasksForDay.First();
        Assert.False(wrapper.IsTracking);
        Assert.False(wrapper.IsOnBreak);
    }

    [Fact]
    public async Task RefreshDailyStatsAsync_CalculatesPercentagesCorrectly()
    {
        var calendarManager = new MockCalendarViewManager();
        var timeTracker = new MockTimeTracker();
        var taskManager = new MockTaskManager();
        var notificationService = new MockNotificationService();
        var logger = new MockLogger();

        var task1 = new TaskEntity { Id = Guid.NewGuid(), Description = "Task 1" };
        var task2 = new TaskEntity { Id = Guid.NewGuid(), Description = "Task 2" };
        calendarManager.TasksToReturn.Add(task1);
        calendarManager.TasksToReturn.Add(task2);

        timeTracker.TotalDurations[task1.Id] = TimeSpan.FromHours(3);
        timeTracker.TotalDurations[task2.Id] = TimeSpan.FromHours(1);

        var viewModel = new DayViewViewModel(calendarManager, timeTracker, taskManager, notificationService, logger);
        await viewModel.LoadAsync();

        Assert.Equal(2, viewModel.TaskTimeBreakdown.Count);
        Assert.Equal(75.0, viewModel.TaskTimeBreakdown[0].PercentageOfDay, 1);
        Assert.Equal(25.0, viewModel.TaskTimeBreakdown[1].PercentageOfDay, 1);
    }

    [Fact]
    public async Task RefreshDailyStatsAsync_SortsByDurationDescending()
    {
        var calendarManager = new MockCalendarViewManager();
        var timeTracker = new MockTimeTracker();
        var taskManager = new MockTaskManager();
        var notificationService = new MockNotificationService();
        var logger = new MockLogger();

        var task1 = new TaskEntity { Id = Guid.NewGuid(), Description = "Task 1" };
        var task2 = new TaskEntity { Id = Guid.NewGuid(), Description = "Task 2" };
        var task3 = new TaskEntity { Id = Guid.NewGuid(), Description = "Task 3" };
        calendarManager.TasksToReturn.Add(task1);
        calendarManager.TasksToReturn.Add(task2);
        calendarManager.TasksToReturn.Add(task3);

        timeTracker.TotalDurations[task1.Id] = TimeSpan.FromHours(1);
        timeTracker.TotalDurations[task2.Id] = TimeSpan.FromHours(3);
        timeTracker.TotalDurations[task3.Id] = TimeSpan.FromHours(2);

        var viewModel = new DayViewViewModel(calendarManager, timeTracker, taskManager, notificationService, logger);
        await viewModel.LoadAsync();

        Assert.Equal("Task 2", viewModel.TaskTimeBreakdown[0].TaskDescription);
        Assert.Equal("Task 3", viewModel.TaskTimeBreakdown[1].TaskDescription);
        Assert.Equal("Task 1", viewModel.TaskTimeBreakdown[2].TaskDescription);
    }

    [Fact]
    public async Task StartTrackingAsync_HandlesErrorGracefully()
    {
        var calendarManager = new MockCalendarViewManager();
        var timeTracker = new MockTimeTracker();
        var taskManager = new MockTaskManager();
        var notificationService = new MockNotificationService();
        var logger = new MockLogger();

        var task = new TaskEntity { Id = Guid.NewGuid(), Description = "Test Task" };
        calendarManager.TasksToReturn.Add(task);

        var viewModel = new DayViewViewModel(calendarManager, timeTracker, taskManager, notificationService, logger);
        await viewModel.LoadAsync();

        var invalidTaskId = Guid.NewGuid();
        await viewModel.StartTrackingAsync(invalidTaskId);

        Assert.Empty(notificationService.Errors);
    }

    [Fact]
    public async Task LoadDayAsync_RestoresTrackingState()
    {
        var calendarManager = new MockCalendarViewManager();
        var timeTracker = new MockTimeTracker();
        var taskManager = new MockTaskManager();
        var notificationService = new MockNotificationService();
        var logger = new MockLogger();

        var task = new TaskEntity { Id = Guid.NewGuid(), Description = "Test Task" };
        calendarManager.TasksToReturn.Add(task);

        timeTracker.CurrentSessions[task.Id] = new TimeTrackingSession
        {
            Id = Guid.NewGuid(),
            TaskId = task.Id,
            StartTime = DateTime.UtcNow,
            Breaks = new List<BreakPeriod>()
        };

        var viewModel = new DayViewViewModel(calendarManager, timeTracker, taskManager, notificationService, logger);
        await viewModel.LoadAsync();

        var wrapper = viewModel.TasksForDay.First();
        Assert.True(wrapper.IsTracking);
    }

    [Fact]
    public async Task LoadDayAsync_RestoresBreakState()
    {
        var calendarManager = new MockCalendarViewManager();
        var timeTracker = new MockTimeTracker();
        var taskManager = new MockTaskManager();
        var notificationService = new MockNotificationService();
        var logger = new MockLogger();

        var task = new TaskEntity { Id = Guid.NewGuid(), Description = "Test Task" };
        calendarManager.TasksToReturn.Add(task);

        timeTracker.CurrentSessions[task.Id] = new TimeTrackingSession
        {
            Id = Guid.NewGuid(),
            TaskId = task.Id,
            StartTime = DateTime.UtcNow,
            Breaks = new List<BreakPeriod>
            {
                new BreakPeriod { Id = Guid.NewGuid(), StartTime = DateTime.UtcNow, EndTime = null }
            }
        };

        var viewModel = new DayViewViewModel(calendarManager, timeTracker, taskManager, notificationService, logger);
        await viewModel.LoadAsync();

        var wrapper = viewModel.TasksForDay.First();
        Assert.True(wrapper.IsTracking);
        Assert.True(wrapper.IsOnBreak);
    }
}
