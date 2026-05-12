using System;

namespace DayPlannerApp.Models;

public class TaskTimeStatistic
{
    private TimeSpan _totalDuration;
    private double _percentageOfDay;

    public Guid TaskId { get; set; }
    public string TaskDescription { get; set; } = string.Empty;

    public TimeSpan TotalDuration
    {
        get => _totalDuration;
        set
        {
            if (value < TimeSpan.Zero)
                throw new ArgumentException("TotalDuration cannot be negative", nameof(value));
            _totalDuration = value;
        }
    }

    public double PercentageOfDay
    {
        get => _percentageOfDay;
        set
        {
            if (value < 0 || value > 100)
                throw new ArgumentException("PercentageOfDay must be between 0 and 100", nameof(value));
            _percentageOfDay = value;
        }
    }

    public string FormattedDuration
    {
        get
        {
            var hours = (int)TotalDuration.TotalHours;
            var minutes = TotalDuration.Minutes;

            if (hours == 0 && minutes == 0)
                return "0m";
            if (hours == 0)
                return $"{minutes}m";
            if (minutes == 0)
                return $"{hours}h";
            return $"{hours}h {minutes}m";
        }
    }
}
