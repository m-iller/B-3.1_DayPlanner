using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DayPlannerApp.Models;
using DayPlannerApp.Services;
using DayPlannerApp.ViewModels;

namespace DayPlannerApp.Tests;

public class TaskTimeTrackingWrapperTests
{
    private class MockCalendarViewManager : ICalendarViewManager
    {
        public Task<IEnumerable<TaskEntity>> GetDayViewAsync(DateTime date) => 
            Task.FromResult<IEnumerable<TaskEntity>>(new List<TaskEntity>());
        public Task<IEnumerable<TaskEntity>> GetWeekViewAsync(DateTime startDate) => 
            Task.FromResult<IEnumerable<TaskEntity>>(new List<TaskEntity>());
        public Task<IEnumerable<TaskEntity>> GetTasksForDateAsync(DateTime date) => 
            Task.FromResult<IEnumerable<TaskEntity>>(new List<TaskEntity>());
    }

    private class MockTimeTracker : ITimeTracker
    {
        public Task StartTrackingAsync(Guid taskId) => Task.CompletedTask;
        public Task StopTrackingAsync(Guid taskId) => Task.CompletedTask;
        public Task StartBreakAsync(Guid taskId) => Task.CompletedTask;
        public Task EndBreakAsync(Guid taskId) => Task.CompletedTask;
        public TimeSpan GetElapsedTime(Guid taskId) => TimeSpan.Zero;
        public TimeSpan GetBreakTime(Guid taskId) => TimeSpan.Zero;
        public TimeTrackingSession? GetCurrentSession(Guid taskId) => null;
        public Task<TimeSpan> GetTotalTaskDurationAsync(Guid taskId) => Task.FromResult(TimeSpan.Zero);
        public Task<TimeSpan> GetTotalBreakTimeAsync(Guid taskId) => Task.FromResult(TimeSpan.Zero);
        public Task StopAllActiveSessionsAsync() => Task.CompletedTask;
    }

    private class MockNotificationService : INotificationService
    {
        public void ShowError(string message, string title = "Error") { }
        public void ShowWarning(string message, string title = "Warning") { }
        public void ShowInfo(string message, string title = "Information") { }
        public bool ShowConfirmation(string message, string title = "Confirm") => true;
    }

    private class MockLogger : ILogger
    {
        public void Debug(string message) { }
        public void Info(string message) { }
        public void Warning(string message) { }
        public void Error(string message, Exception? exception = null) { }
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
    public void DisplayTime_FormatsCorrectly_ForZeroTime()
    {
        var task = new TaskEntity { Id = Guid.NewGuid(), Description = "Test" };
        var viewModel = new DayViewViewModel(
            new MockCalendarViewManager(),
            new MockTimeTracker(),
            new MockTaskManager(),
            new MockNotificationService(),
            new MockLogger()
        );
        var wrapper = new TaskTimeTrackingWrapper(task, viewModel);

        wrapper.ElapsedTime = TimeSpan.Zero;

        Assert.Equal("00:00:00", wrapper.DisplayTime);
    }

    [Fact]
    public void DisplayTime_FormatsCorrectly_ForOneHour()
    {
        var task = new TaskEntity { Id = Guid.NewGuid(), Description = "Test" };
        var viewModel = new DayViewViewModel(
            new MockCalendarViewManager(),
            new MockTimeTracker(),
            new MockTaskManager(),
            new MockNotificationService(),
            new MockLogger()
        );
        var wrapper = new TaskTimeTrackingWrapper(task, viewModel);

        wrapper.ElapsedTime = TimeSpan.FromHours(1);

        Assert.Equal("01:00:00", wrapper.DisplayTime);
    }

    [Fact]
    public void DisplayTime_FormatsCorrectly_ForComplexTime()
    {
        var task = new TaskEntity { Id = Guid.NewGuid(), Description = "Test" };
        var viewModel = new DayViewViewModel(
            new MockCalendarViewManager(),
            new MockTimeTracker(),
            new MockTaskManager(),
            new MockNotificationService(),
            new MockLogger()
        );
        var wrapper = new TaskTimeTrackingWrapper(task, viewModel);

        wrapper.ElapsedTime = new TimeSpan(2, 15, 45);

        Assert.Equal("02:15:45", wrapper.DisplayTime);
    }

    [Fact]
    public void DisplayTime_FormatsCorrectly_ForMoreThan24Hours()
    {
        var task = new TaskEntity { Id = Guid.NewGuid(), Description = "Test" };
        var viewModel = new DayViewViewModel(
            new MockCalendarViewManager(),
            new MockTimeTracker(),
            new MockTaskManager(),
            new MockNotificationService(),
            new MockLogger()
        );
        var wrapper = new TaskTimeTrackingWrapper(task, viewModel);

        wrapper.ElapsedTime = new TimeSpan(25, 30, 15);

        Assert.Equal("25:30:15", wrapper.DisplayTime);
    }

    [Fact]
    public void TrackingStateText_ReturnsNotStarted_WhenNotTracking()
    {
        var task = new TaskEntity { Id = Guid.NewGuid(), Description = "Test" };
        var viewModel = new DayViewViewModel(
            new MockCalendarViewManager(),
            new MockTimeTracker(),
            new MockTaskManager(),
            new MockNotificationService(),
            new MockLogger()
        );
        var wrapper = new TaskTimeTrackingWrapper(task, viewModel);

        wrapper.IsTracking = false;

        Assert.Equal("Not Started", wrapper.TrackingStateText);
    }

    [Fact]
    public void TrackingStateText_ReturnsTracking_WhenTrackingAndNotOnBreak()
    {
        var task = new TaskEntity { Id = Guid.NewGuid(), Description = "Test" };
        var viewModel = new DayViewViewModel(
            new MockCalendarViewManager(),
            new MockTimeTracker(),
            new MockTaskManager(),
            new MockNotificationService(),
            new MockLogger()
        );
        var wrapper = new TaskTimeTrackingWrapper(task, viewModel);

        wrapper.IsTracking = true;
        wrapper.IsOnBreak = false;

        Assert.Equal("Tracking", wrapper.TrackingStateText);
    }

    [Fact]
    public void TrackingStateText_ReturnsOnBreak_WhenTrackingAndOnBreak()
    {
        var task = new TaskEntity { Id = Guid.NewGuid(), Description = "Test" };
        var viewModel = new DayViewViewModel(
            new MockCalendarViewManager(),
            new MockTimeTracker(),
            new MockTaskManager(),
            new MockNotificationService(),
            new MockLogger()
        );
        var wrapper = new TaskTimeTrackingWrapper(task, viewModel);

        wrapper.IsTracking = true;
        wrapper.IsOnBreak = true;

        Assert.Equal("On Break", wrapper.TrackingStateText);
    }

    [Fact]
    public void StartTrackingCommand_CanExecute_WhenNotTracking()
    {
        var task = new TaskEntity { Id = Guid.NewGuid(), Description = "Test" };
        var viewModel = new DayViewViewModel(
            new MockCalendarViewManager(),
            new MockTimeTracker(),
            new MockTaskManager(),
            new MockNotificationService(),
            new MockLogger()
        );
        var wrapper = new TaskTimeTrackingWrapper(task, viewModel);

        wrapper.IsTracking = false;

        Assert.True(wrapper.StartTrackingCommand.CanExecute(null));
    }

    [Fact]
    public void StartTrackingCommand_CannotExecute_WhenTracking()
    {
        var task = new TaskEntity { Id = Guid.NewGuid(), Description = "Test" };
        var viewModel = new DayViewViewModel(
            new MockCalendarViewManager(),
            new MockTimeTracker(),
            new MockTaskManager(),
            new MockNotificationService(),
            new MockLogger()
        );
        var wrapper = new TaskTimeTrackingWrapper(task, viewModel);

        wrapper.IsTracking = true;

        Assert.False(wrapper.StartTrackingCommand.CanExecute(null));
    }

    [Fact]
    public void PauseTrackingCommand_CanExecute_WhenTrackingAndNotOnBreak()
    {
        var task = new TaskEntity { Id = Guid.NewGuid(), Description = "Test" };
        var viewModel = new DayViewViewModel(
            new MockCalendarViewManager(),
            new MockTimeTracker(),
            new MockTaskManager(),
            new MockNotificationService(),
            new MockLogger()
        );
        var wrapper = new TaskTimeTrackingWrapper(task, viewModel);

        wrapper.IsTracking = true;
        wrapper.IsOnBreak = false;

        Assert.True(wrapper.PauseTrackingCommand.CanExecute(null));
    }

    [Fact]
    public void PauseTrackingCommand_CannotExecute_WhenNotTracking()
    {
        var task = new TaskEntity { Id = Guid.NewGuid(), Description = "Test" };
        var viewModel = new DayViewViewModel(
            new MockCalendarViewManager(),
            new MockTimeTracker(),
            new MockTaskManager(),
            new MockNotificationService(),
            new MockLogger()
        );
        var wrapper = new TaskTimeTrackingWrapper(task, viewModel);

        wrapper.IsTracking = false;

        Assert.False(wrapper.PauseTrackingCommand.CanExecute(null));
    }

    [Fact]
    public void PauseTrackingCommand_CannotExecute_WhenOnBreak()
    {
        var task = new TaskEntity { Id = Guid.NewGuid(), Description = "Test" };
        var viewModel = new DayViewViewModel(
            new MockCalendarViewManager(),
            new MockTimeTracker(),
            new MockTaskManager(),
            new MockNotificationService(),
            new MockLogger()
        );
        var wrapper = new TaskTimeTrackingWrapper(task, viewModel);

        wrapper.IsTracking = true;
        wrapper.IsOnBreak = true;

        Assert.False(wrapper.PauseTrackingCommand.CanExecute(null));
    }

    [Fact]
    public void ResumeTrackingCommand_CanExecute_WhenTrackingAndOnBreak()
    {
        var task = new TaskEntity { Id = Guid.NewGuid(), Description = "Test" };
        var viewModel = new DayViewViewModel(
            new MockCalendarViewManager(),
            new MockTimeTracker(),
            new MockTaskManager(),
            new MockNotificationService(),
            new MockLogger()
        );
        var wrapper = new TaskTimeTrackingWrapper(task, viewModel);

        wrapper.IsTracking = true;
        wrapper.IsOnBreak = true;

        Assert.True(wrapper.ResumeTrackingCommand.CanExecute(null));
    }

    [Fact]
    public void ResumeTrackingCommand_CannotExecute_WhenNotOnBreak()
    {
        var task = new TaskEntity { Id = Guid.NewGuid(), Description = "Test" };
        var viewModel = new DayViewViewModel(
            new MockCalendarViewManager(),
            new MockTimeTracker(),
            new MockTaskManager(),
            new MockNotificationService(),
            new MockLogger()
        );
        var wrapper = new TaskTimeTrackingWrapper(task, viewModel);

        wrapper.IsTracking = true;
        wrapper.IsOnBreak = false;

        Assert.False(wrapper.ResumeTrackingCommand.CanExecute(null));
    }

    [Fact]
    public void CompleteTrackingCommand_CanExecute_WhenTracking()
    {
        var task = new TaskEntity { Id = Guid.NewGuid(), Description = "Test" };
        var viewModel = new DayViewViewModel(
            new MockCalendarViewManager(),
            new MockTimeTracker(),
            new MockTaskManager(),
            new MockNotificationService(),
            new MockLogger()
        );
        var wrapper = new TaskTimeTrackingWrapper(task, viewModel);

        wrapper.IsTracking = true;

        Assert.True(wrapper.CompleteTrackingCommand.CanExecute(null));
    }

    [Fact]
    public void CompleteTrackingCommand_CannotExecute_WhenNotTracking()
    {
        var task = new TaskEntity { Id = Guid.NewGuid(), Description = "Test" };
        var viewModel = new DayViewViewModel(
            new MockCalendarViewManager(),
            new MockTimeTracker(),
            new MockTaskManager(),
            new MockNotificationService(),
            new MockLogger()
        );
        var wrapper = new TaskTimeTrackingWrapper(task, viewModel);

        wrapper.IsTracking = false;

        Assert.False(wrapper.CompleteTrackingCommand.CanExecute(null));
    }

    [Fact]
    public void PropertyChanged_Fires_WhenElapsedTimeChanges()
    {
        var task = new TaskEntity { Id = Guid.NewGuid(), Description = "Test" };
        var viewModel = new DayViewViewModel(
            new MockCalendarViewManager(),
            new MockTimeTracker(),
            new MockTaskManager(),
            new MockNotificationService(),
            new MockLogger()
        );
        var wrapper = new TaskTimeTrackingWrapper(task, viewModel);

        var propertyChangedFired = false;
        wrapper.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(wrapper.ElapsedTime))
                propertyChangedFired = true;
        };

        wrapper.ElapsedTime = TimeSpan.FromMinutes(5);

        Assert.True(propertyChangedFired);
    }

    [Fact]
    public void PropertyChanged_FiresForDisplayTime_WhenElapsedTimeChanges()
    {
        var task = new TaskEntity { Id = Guid.NewGuid(), Description = "Test" };
        var viewModel = new DayViewViewModel(
            new MockCalendarViewManager(),
            new MockTimeTracker(),
            new MockTaskManager(),
            new MockNotificationService(),
            new MockLogger()
        );
        var wrapper = new TaskTimeTrackingWrapper(task, viewModel);

        var displayTimeChangedFired = false;
        wrapper.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(wrapper.DisplayTime))
                displayTimeChangedFired = true;
        };

        wrapper.ElapsedTime = TimeSpan.FromMinutes(5);

        Assert.True(displayTimeChangedFired);
    }
}
