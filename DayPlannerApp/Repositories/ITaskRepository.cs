using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DayPlannerApp.Models;

namespace DayPlannerApp.Repositories;

public interface ITaskRepository
{
    Task<TaskEntity> InsertAsync(TaskEntity task);
    Task<TaskEntity> UpdateAsync(TaskEntity task);
    Task DeleteAsync(Guid taskId);
    Task<TaskEntity?> GetByIdAsync(Guid taskId);
    Task<IEnumerable<TaskEntity>> QueryAsync(TaskQuerySpec spec);
}
