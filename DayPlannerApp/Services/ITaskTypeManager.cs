using System.Collections.Generic;
using System.Threading.Tasks;
using DayPlannerApp.Models;

namespace DayPlannerApp.Services;

public interface ITaskTypeManager
{
    Task<TaskType?> GetTaskTypeAsync(int typeId);
    Task<IEnumerable<TaskType>> GetAllTaskTypesAsync();
    Task<TaskType> CreateTaskTypeAsync(TaskType taskType);
    Task UpdateTaskTypeNameAsync(int typeId, string newName);
    Task DeleteTaskTypeAsync(int typeId);
}
