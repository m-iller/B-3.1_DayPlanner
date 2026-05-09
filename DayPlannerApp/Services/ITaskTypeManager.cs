using System.Collections.Generic;
using System.Threading.Tasks;
using DayPlannerApp.Models;

namespace DayPlannerApp.Services;

public interface ITaskTypeManager
{
    Task<TaskType?> GetTaskTypeAsync(int typeId);
    Task<IEnumerable<TaskType>> GetAllTaskTypesAsync();
    Task UpdateTaskTypeNameAsync(int typeId, string newName);
}
