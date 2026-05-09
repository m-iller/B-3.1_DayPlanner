using System;
using System.Threading.Tasks;
using DayPlannerApp.Models;

namespace DayPlannerApp.Services;

public interface ITimeTracker
{
    Task StartTrackingAsync(Guid taskId);
    Task StopTrackingAsync(Guid taskId);
    Task StartBreakAsync(Guid taskId);
    Task EndBreakAsync(Guid taskId);
    TimeSpan GetElapsedTime(Guid taskId);
    TimeSpan GetBreakTime(Guid taskId);
    TimeTrackingSession? GetCurrentSession(Guid taskId);
    Task<TimeSpan> GetTotalTaskDurationAsync(Guid taskId);
    Task<TimeSpan> GetTotalBreakTimeAsync(Guid taskId);
}
