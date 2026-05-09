using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DayPlannerApp.Models;

namespace DayPlannerApp.Services;

public interface ICoordinateController
{
    Task UpdateTaskCoordinatesAsync(Guid taskId, double importance, double complexity);
    Task<IEnumerable<TaskEntity>> GetTasksByCoordinateRangeAsync(
        double minImportance, double maxImportance,
        double minComplexity, double maxComplexity);
}
