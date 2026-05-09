using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DayPlannerApp.Models;
using DayPlannerApp.Repositories;

namespace DayPlannerApp.Services;

public class CalendarViewManager : ICalendarViewManager
{
    private readonly ITaskRepository _taskRepository;
    private const int DAYS_IN_WEEK = 7;

    public CalendarViewManager(ITaskRepository taskRepository)
    {
        _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
    }

    public async Task<IEnumerable<TaskEntity>> GetDayViewAsync(DateTime date)
    {
        return await GetTasksForDateAsync(date);
    }

    public async Task<IEnumerable<TaskEntity>> GetWeekViewAsync(DateTime weekStart)
    {
        var weekEnd = weekStart.AddDays(DAYS_IN_WEEK - 1);

        var spec = new TaskQuerySpec
        {
            StartDate = weekStart.Date,
            EndDate = weekEnd.Date
        };

        return await _taskRepository.QueryAsync(spec);
    }

    public async Task<IEnumerable<TaskEntity>> GetTasksForDateAsync(DateTime date)
    {
        var spec = new TaskQuerySpec
        {
            StartDate = date.Date,
            EndDate = date.Date
        };

        return await _taskRepository.QueryAsync(spec);
    }
}
