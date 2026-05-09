using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DayPlannerApp.Models;
using DayPlannerApp.Repositories;

namespace DayPlannerApp.Services;

public class TimeTracker : ITimeTracker
{
    private readonly ITimeTrackingRepository _repository;
    private readonly Dictionary<Guid, TrackingState> _activeTracking = new();

    public TimeTracker(ITimeTrackingRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task StartTrackingAsync(Guid taskId)
    {
        if (_activeTracking.ContainsKey(taskId))
        {
            throw new InvalidOperationException($"Tracking already active for task {taskId}");
        }

        var session = new TimeTrackingSession
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            StartTime = DateTime.UtcNow,
            Breaks = new List<BreakPeriod>()
        };

        var state = new TrackingState
        {
            Session = session,
            WorkStopwatch = Stopwatch.StartNew(),
            BreakStopwatch = new Stopwatch()
        };

        _activeTracking[taskId] = state;
        await _repository.InsertSessionAsync(session);
    }

    public async Task StopTrackingAsync(Guid taskId)
    {
        if (!_activeTracking.TryGetValue(taskId, out var state))
        {
            throw new InvalidOperationException($"No active tracking for task {taskId}");
        }

        if (state.IsOnBreak)
        {
            throw new InvalidOperationException("Cannot stop tracking while on break. End break first.");
        }

        state.WorkStopwatch.Stop();
        state.Session.EndTime = DateTime.UtcNow;
        state.Session.TotalDuration = state.WorkStopwatch.Elapsed;
        state.Session.TotalBreakTime = state.BreakStopwatch.Elapsed;

        await _repository.UpdateSessionAsync(state.Session);
        _activeTracking.Remove(taskId);
    }

    public async Task StartBreakAsync(Guid taskId)
    {
        if (!_activeTracking.TryGetValue(taskId, out var state))
        {
            throw new InvalidOperationException($"No active tracking for task {taskId}");
        }

        if (state.IsOnBreak)
        {
            throw new InvalidOperationException("Already on break");
        }

        state.WorkStopwatch.Stop();
        state.BreakStopwatch.Start();
        state.IsOnBreak = true;

        var breakPeriod = new BreakPeriod
        {
            Id = Guid.NewGuid(),
            StartTime = DateTime.UtcNow
        };

        state.Session.Breaks.Add(breakPeriod);
        await _repository.UpdateSessionAsync(state.Session);
    }

    public async Task EndBreakAsync(Guid taskId)
    {
        if (!_activeTracking.TryGetValue(taskId, out var state))
        {
            throw new InvalidOperationException($"No active tracking for task {taskId}");
        }

        if (!state.IsOnBreak)
        {
            throw new InvalidOperationException("Not currently on break");
        }

        state.BreakStopwatch.Stop();
        state.WorkStopwatch.Start();
        state.IsOnBreak = false;

        var currentBreak = state.Session.Breaks.Last();
        currentBreak.EndTime = DateTime.UtcNow;

        await _repository.UpdateSessionAsync(state.Session);
    }

    public TimeSpan GetElapsedTime(Guid taskId)
    {
        if (!_activeTracking.TryGetValue(taskId, out var state))
        {
            return TimeSpan.Zero;
        }

        return state.WorkStopwatch.Elapsed;
    }

    public TimeSpan GetBreakTime(Guid taskId)
    {
        if (!_activeTracking.TryGetValue(taskId, out var state))
        {
            return TimeSpan.Zero;
        }

        return state.BreakStopwatch.Elapsed;
    }

    public TimeTrackingSession? GetCurrentSession(Guid taskId)
    {
        if (!_activeTracking.TryGetValue(taskId, out var state))
        {
            return null;
        }

        return state.Session;
    }

    public async Task<TimeSpan> GetTotalTaskDurationAsync(Guid taskId)
    {
        var sessions = await _repository.GetSessionsByTaskIdAsync(taskId);
        var total = TimeSpan.Zero;

        foreach (var session in sessions)
        {
            total += session.TotalDuration;
        }

        if (_activeTracking.TryGetValue(taskId, out var state))
        {
            total += state.WorkStopwatch.Elapsed;
        }

        return total;
    }

    public async Task<TimeSpan> GetTotalBreakTimeAsync(Guid taskId)
    {
        var sessions = await _repository.GetSessionsByTaskIdAsync(taskId);
        var total = TimeSpan.Zero;

        foreach (var session in sessions)
        {
            total += session.TotalBreakTime;
        }

        if (_activeTracking.TryGetValue(taskId, out var state))
        {
            total += state.BreakStopwatch.Elapsed;
        }

        return total;
    }

    private class TrackingState
    {
        public TimeTrackingSession Session { get; set; } = null!;
        public Stopwatch WorkStopwatch { get; set; } = null!;
        public Stopwatch BreakStopwatch { get; set; } = null!;
        public bool IsOnBreak { get; set; }
    }
}
