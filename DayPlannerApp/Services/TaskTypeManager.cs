using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DayPlannerApp.Models;
using DayPlannerApp.Repositories;

namespace DayPlannerApp.Services;

public class TaskTypeManager : ITaskTypeManager
{
    private readonly ITaskTypeRepository _repository;

    public TaskTypeManager(ITaskTypeRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<TaskType?> GetTaskTypeAsync(int typeId)
    {
        return await _repository.GetTaskTypeAsync(typeId);
    }

    public async Task<IEnumerable<TaskType>> GetAllTaskTypesAsync()
    {
        return await _repository.GetAllTaskTypesAsync();
    }

    public async Task UpdateTaskTypeNameAsync(int typeId, string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            throw new ArgumentException("Task type name cannot be empty", nameof(newName));
        }

        await _repository.UpdateTaskTypeNameAsync(typeId, newName);
    }
}
