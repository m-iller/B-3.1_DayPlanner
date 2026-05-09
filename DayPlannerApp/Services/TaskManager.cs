using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DayPlannerApp.Models;
using DayPlannerApp.Repositories;

namespace DayPlannerApp.Services;

public class TaskManager : ITaskManager
{
    private readonly ITaskRepository _taskRepository;
    private readonly ILogger _logger;

    public TaskManager(ITaskRepository taskRepository, ILogger logger)
    {
        _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TaskEntity> CreateTaskAsync(TaskEntity task)
    {
        try
        {
            ValidateTask(task);
            
            task.Id = Guid.NewGuid();
            task.CreatedAt = DateTime.UtcNow;
            task.UpdatedAt = DateTime.UtcNow;
            
            var result = await _taskRepository.InsertAsync(task);
            _logger.Info($"Task created: {task.Id}");
            return result;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to create task", ex);
            throw;
        }
    }

    public async Task<TaskEntity> UpdateTaskAsync(TaskEntity task)
    {
        try
        {
            ValidateTask(task);
            
            task.UpdatedAt = DateTime.UtcNow;
            
            var result = await _taskRepository.UpdateAsync(task);
            _logger.Info($"Task updated: {task.Id}");
            return result;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to update task {task.Id}", ex);
            throw;
        }
    }

    public async Task DeleteTaskAsync(Guid taskId)
    {
        try
        {
            await _taskRepository.DeleteAsync(taskId);
            _logger.Info($"Task deleted: {taskId}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to delete task {taskId}", ex);
            throw;
        }
    }

    public async Task<TaskEntity?> GetTaskByIdAsync(Guid taskId)
    {
        return await _taskRepository.GetByIdAsync(taskId);
    }

    public async Task<IEnumerable<TaskEntity>> GetTasksByDateRangeAsync(DateTime start, DateTime end)
    {
        var spec = new TaskQuerySpec
        {
            StartDate = start,
            EndDate = end
        };
        
        return await _taskRepository.QueryAsync(spec);
    }

    public async Task<IEnumerable<TaskEntity>> GetTasksByTagsAsync(IEnumerable<string> tags)
    {
        var spec = new TaskQuerySpec
        {
            Tags = new List<string>(tags)
        };
        
        return await _taskRepository.QueryAsync(spec);
    }

    public async Task<IEnumerable<TaskEntity>> GetTasksByTypeAsync(int taskTypeId)
    {
        var spec = new TaskQuerySpec
        {
            TaskTypeId = taskTypeId
        };
        
        return await _taskRepository.QueryAsync(spec);
    }

    public async Task<IEnumerable<TaskEntity>> QueryTasksAsync(TaskQuerySpec spec)
    {
        return await _taskRepository.QueryAsync(spec);
    }

    private void ValidateTask(TaskEntity task)
    {
        if (string.IsNullOrWhiteSpace(task.Description))
        {
            throw new ArgumentException("Task description is required.", nameof(task));
        }

        if (task.Importance.HasValue && (task.Importance.Value < 0 || task.Importance.Value > 100))
        {
            throw new ArgumentException("Importance must be between 0 and 100.", nameof(task));
        }

        if (task.Complexity.HasValue && (task.Complexity.Value < 0 || task.Complexity.Value > 100))
        {
            throw new ArgumentException("Complexity must be between 0 and 100.", nameof(task));
        }
    }
}
