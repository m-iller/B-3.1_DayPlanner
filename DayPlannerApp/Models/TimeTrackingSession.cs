using System;
using System.Collections.Generic;

namespace DayPlannerApp.Models;

public class TimeTrackingSession
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public List<BreakPeriod> Breaks { get; set; } = new();
    public TimeSpan TotalDuration { get; set; }
    public TimeSpan TotalBreakTime { get; set; }
}

public class BreakPeriod
{
    public Guid Id { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
}
