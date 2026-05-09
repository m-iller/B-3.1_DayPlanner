using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DayPlannerApp.Models;

namespace DayPlannerApp.Services;

public interface ICalendarViewManager
{
    Task<IEnumerable<TaskEntity>> GetDayViewAsync(DateTime date);
    Task<IEnumerable<TaskEntity>> GetWeekViewAsync(DateTime weekStart);
    Task<IEnumerable<TaskEntity>> GetTasksForDateAsync(DateTime date);
}
