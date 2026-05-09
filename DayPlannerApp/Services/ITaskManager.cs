using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DayPlannerApp.Models;

namespace DayPlannerApp.Services;

public interface ITaskManager
{
    Task<TaskEntity> CreateTaskAsync(TaskEntity task);
    Task<TaskEntity> UpdateTaskAsync(TaskEntity task);
    Task DeleteTaskAsync(Guid taskId);
    Task<TaskEntity?> GetTaskByIdAsync(Guid taskId);
    Task<IEnumerable<TaskEntity>> GetTasksByDateRangeAsync(DateTime start, DateTime end);
    Task<IEnumerable<TaskEntity>> GetTasksByTagsAsync(IEnumerable<string> tags);
    Task<IEnumerable<TaskEntity>> GetTasksByTypeAsync(int taskTypeId);
    Task<IEnumerable<TaskEntity>> QueryTasksAsync(TaskQuerySpec spec);
}
