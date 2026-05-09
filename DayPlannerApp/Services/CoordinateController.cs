using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DayPlannerApp.Models;
using DayPlannerApp.Repositories;

namespace DayPlannerApp.Services;

public class CoordinateController : ICoordinateController
{
    private readonly ITaskRepository _taskRepository;
    private const double MIN_COORDINATE = 0.0;
    private const double MAX_COORDINATE = 100.0;

    public CoordinateController(ITaskRepository taskRepository)
    {
        _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
    }

    public async Task UpdateTaskCoordinatesAsync(Guid taskId, double importance, double complexity)
    {
        ValidateCoordinate(importance, nameof(importance));
        ValidateCoordinate(complexity, nameof(complexity));

        var task = await _taskRepository.GetByIdAsync(taskId);
        if (task == null)
        {
            throw new InvalidOperationException($"Task with ID {taskId} not found");
        }

        task.Importance = importance;
        task.Complexity = complexity;
        task.UpdatedAt = DateTime.UtcNow;

        await _taskRepository.UpdateAsync(task);
    }

    public async Task<IEnumerable<TaskEntity>> GetTasksByCoordinateRangeAsync(
        double minImportance, double maxImportance,
        double minComplexity, double maxComplexity)
    {
        ValidateCoordinate(minImportance, nameof(minImportance));
        ValidateCoordinate(maxImportance, nameof(maxImportance));
        ValidateCoordinate(minComplexity, nameof(minComplexity));
        ValidateCoordinate(maxComplexity, nameof(maxComplexity));

        if (minImportance > maxImportance)
        {
            throw new ArgumentException("minImportance cannot be greater than maxImportance");
        }

        if (minComplexity > maxComplexity)
        {
            throw new ArgumentException("minComplexity cannot be greater than maxComplexity");
        }

        var spec = new TaskQuerySpec
        {
            MinImportance = minImportance,
            MaxImportance = maxImportance,
            MinComplexity = minComplexity,
            MaxComplexity = maxComplexity
        };

        return await _taskRepository.QueryAsync(spec);
    }

    private void ValidateCoordinate(double value, string paramName)
    {
        if (value < MIN_COORDINATE || value > MAX_COORDINATE)
        {
            throw new ArgumentException(
                $"{paramName} must be between {MIN_COORDINATE} and {MAX_COORDINATE}",
                paramName);
        }
    }
}
