using System;
using DayPlannerApp.Models;

namespace DayPlannerApp.Tests;

public class TaskTimeStatisticTests
{
    [Fact]
    public void FormattedDuration_ReturnsZeroMinutes_ForZeroTime()
    {
        var stat = new TaskTimeStatistic
        {
            TaskId = Guid.NewGuid(),
            TaskDescription = "Test",
            TotalDuration = TimeSpan.Zero
        };

        Assert.Equal("0m", stat.FormattedDuration);
    }

    [Fact]
    public void FormattedDuration_ReturnsSeconds_ForLessThanOneMinute()
    {
        var stat = new TaskTimeStatistic
        {
            TaskId = Guid.NewGuid(),
            TaskDescription = "Test",
            TotalDuration = TimeSpan.FromSeconds(45)
        };

        Assert.Equal("0m", stat.FormattedDuration);
    }

    [Fact]
    public void FormattedDuration_ReturnsMinutes_ForLessThanOneHour()
    {
        var stat = new TaskTimeStatistic
        {
            TaskId = Guid.NewGuid(),
            TaskDescription = "Test",
            TotalDuration = TimeSpan.FromMinutes(45)
        };

        Assert.Equal("45m", stat.FormattedDuration);
    }

    [Fact]
    public void FormattedDuration_ReturnsHours_ForExactHours()
    {
        var stat = new TaskTimeStatistic
        {
            TaskId = Guid.NewGuid(),
            TaskDescription = "Test",
            TotalDuration = TimeSpan.FromHours(2)
        };

        Assert.Equal("2h", stat.FormattedDuration);
    }

    [Fact]
    public void FormattedDuration_ReturnsHoursAndMinutes_ForComplexTime()
    {
        var stat = new TaskTimeStatistic
        {
            TaskId = Guid.NewGuid(),
            TaskDescription = "Test",
            TotalDuration = new TimeSpan(2, 15, 0)
        };

        Assert.Equal("2h 15m", stat.FormattedDuration);
    }

    [Fact]
    public void FormattedDuration_ReturnsHoursAndMinutes_IgnoringSeconds()
    {
        var stat = new TaskTimeStatistic
        {
            TaskId = Guid.NewGuid(),
            TaskDescription = "Test",
            TotalDuration = new TimeSpan(2, 15, 45)
        };

        Assert.Equal("2h 15m", stat.FormattedDuration);
    }

    [Fact]
    public void TotalDuration_ThrowsException_ForNegativeValue()
    {
        var stat = new TaskTimeStatistic
        {
            TaskId = Guid.NewGuid(),
            TaskDescription = "Test"
        };

        Assert.Throws<ArgumentException>(() => 
            stat.TotalDuration = TimeSpan.FromMinutes(-10));
    }

    [Fact]
    public void PercentageOfDay_ThrowsException_ForNegativeValue()
    {
        var stat = new TaskTimeStatistic
        {
            TaskId = Guid.NewGuid(),
            TaskDescription = "Test",
            TotalDuration = TimeSpan.FromHours(1)
        };

        Assert.Throws<ArgumentException>(() => 
            stat.PercentageOfDay = -5);
    }

    [Fact]
    public void PercentageOfDay_ThrowsException_ForValueOver100()
    {
        var stat = new TaskTimeStatistic
        {
            TaskId = Guid.NewGuid(),
            TaskDescription = "Test",
            TotalDuration = TimeSpan.FromHours(1)
        };

        Assert.Throws<ArgumentException>(() => 
            stat.PercentageOfDay = 105);
    }

    [Fact]
    public void PercentageOfDay_AcceptsZero()
    {
        var stat = new TaskTimeStatistic
        {
            TaskId = Guid.NewGuid(),
            TaskDescription = "Test",
            TotalDuration = TimeSpan.FromHours(1),
            PercentageOfDay = 0
        };

        Assert.Equal(0, stat.PercentageOfDay);
    }

    [Fact]
    public void PercentageOfDay_Accepts100()
    {
        var stat = new TaskTimeStatistic
        {
            TaskId = Guid.NewGuid(),
            TaskDescription = "Test",
            TotalDuration = TimeSpan.FromHours(1),
            PercentageOfDay = 100
        };

        Assert.Equal(100, stat.PercentageOfDay);
    }
}
