using System.Collections.Generic;
using System.Threading.Tasks;
using DayPlannerApp.Models;

namespace DayPlannerApp.Repositories;

public interface ITaskTypeRepository
{
    Task<TaskType?> GetTaskTypeAsync(int typeId);
    Task<IEnumerable<TaskType>> GetAllTaskTypesAsync();
    Task<TaskType> InsertTaskTypeAsync(TaskType taskType);
    Task UpdateTaskTypeNameAsync(int typeId, string newName);
    Task DeleteTaskTypeAsync(int typeId);
}
