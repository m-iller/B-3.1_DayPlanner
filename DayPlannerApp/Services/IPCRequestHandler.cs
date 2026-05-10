using System.Text.Json;
using DayPlannerApp.Models;

namespace DayPlannerApp.Services;

/// <summary>
/// Routes IPC requests to appropriate business logic services.
/// Supports REST-like operations (GET/POST/PUT/DELETE) for /tasks, /tags, /time-tracking resources.
/// </summary>
public class IPCRequestHandler : IIPCRequestHandler
{
    private readonly ITaskManager _taskManager;
    private readonly ITagManager _tagManager;
    private readonly ITimeTracker _timeTracker;
    private readonly ILogger _logger;

    public IPCRequestHandler(
        ITaskManager taskManager,
        ITagManager tagManager,
        ITimeTracker timeTracker,
        ILogger logger)
    {
        _taskManager = taskManager ?? throw new ArgumentNullException(nameof(taskManager));
        _tagManager = tagManager ?? throw new ArgumentNullException(nameof(tagManager));
        _timeTracker = timeTracker ?? throw new ArgumentNullException(nameof(timeTracker));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IPCResponse> HandleRequestAsync(IPCRequest request)
    {
        try
        {
            _logger.Debug($"Handling IPC request: {request.Method} {request.Resource}");

            // Parse resource path
            var resourceParts = request.Resource.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (resourceParts.Length == 0)
            {
                return CreateErrorResponse(request.RequestId, 400, "Resource path is empty");
            }

            var resourceType = resourceParts[0].ToLowerInvariant();
            var resourceId = resourceParts.Length > 1 ? resourceParts[1] : null;

            return resourceType switch
            {
                "tasks" => await HandleTasksRequestAsync(request, resourceId),
                "tags" => await HandleTagsRequestAsync(request, resourceId),
                "time-tracking" => await HandleTimeTrackingRequestAsync(request, resourceId),
                _ => CreateErrorResponse(request.RequestId, 404, $"Unknown resource: {resourceType}")
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error handling IPC request: {ex.Message}", ex);
            return CreateErrorResponse(request.RequestId, 500, $"Internal error: {ex.Message}");
        }
    }

    private async Task<IPCResponse> HandleTasksRequestAsync(IPCRequest request, string? resourceId)
    {
        return request.Method.ToUpperInvariant() switch
        {
            "GET" => await HandleGetTasksAsync(request, resourceId),
            "POST" => await HandleCreateTaskAsync(request),
            "PUT" => await HandleUpdateTaskAsync(request, resourceId),
            "DELETE" => await HandleDeleteTaskAsync(request, resourceId),
            _ => CreateErrorResponse(request.RequestId, 405, $"Method {request.Method} not allowed for /tasks")
        };
    }

    private async Task<IPCResponse> HandleGetTasksAsync(IPCRequest request, string? resourceId)
    {
        try
        {
            // GET /tasks/{id} - Get specific task
            if (!string.IsNullOrEmpty(resourceId))
            {
                if (!Guid.TryParse(resourceId, out var taskId))
                {
                    return CreateErrorResponse(request.RequestId, 400, "Invalid task ID format");
                }

                var task = await _taskManager.GetTaskByIdAsync(taskId);
                if (task == null)
                {
                    return CreateErrorResponse(request.RequestId, 404, "Task not found");
                }

                return CreateSuccessResponse(request.RequestId, TaskToDict(task));
            }

            // GET /tasks - Query tasks with filters
            if (request.Body != null)
            {
                // Filter by tags
                if (request.Body.TryGetValue("tags", out var tagsObj) && tagsObj != null)
                {
                    var tags = ParseStringArray(tagsObj);
                    var tasks = await _taskManager.GetTasksByTagsAsync(tags);
                    return CreateSuccessResponse(request.RequestId, new Dictionary<string, object>
                    {
                        ["tasks"] = tasks.Select(TaskToDict).ToList()
                    });
                }

                // Filter by type
                if (request.Body.TryGetValue("typeId", out var typeIdObj) && typeIdObj != null)
                {
                    var typeId = Convert.ToInt32(typeIdObj);
                    var tasks = await _taskManager.GetTasksByTypeAsync(typeId);
                    return CreateSuccessResponse(request.RequestId, new Dictionary<string, object>
                    {
                        ["tasks"] = tasks.Select(TaskToDict).ToList()
                    });
                }

                // Filter by date range
                if (request.Body.TryGetValue("startDate", out var startObj) && 
                    request.Body.TryGetValue("endDate", out var endObj))
                {
                    var startDate = DateTime.Parse(startObj.ToString()!);
                    var endDate = DateTime.Parse(endObj.ToString()!);
                    var tasks = await _taskManager.GetTasksByDateRangeAsync(startDate, endDate);
                    return CreateSuccessResponse(request.RequestId, new Dictionary<string, object>
                    {
                        ["tasks"] = tasks.Select(TaskToDict).ToList()
                    });
                }
            }

            // No filters - return error (avoid returning all tasks)
            return CreateErrorResponse(request.RequestId, 400, "Query filters required (tags, typeId, or date range)");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error in GET /tasks: {ex.Message}", ex);
            return CreateErrorResponse(request.RequestId, 500, $"Error retrieving tasks: {ex.Message}");
        }
    }

    private async Task<IPCResponse> HandleCreateTaskAsync(IPCRequest request)
    {
        try
        {
            if (request.Body == null)
            {
                return CreateErrorResponse(request.RequestId, 400, "Request body is required");
            }

            var task = DictToTask(request.Body);
            var createdTask = await _taskManager.CreateTaskAsync(task);

            return CreateSuccessResponse(request.RequestId, TaskToDict(createdTask), 201);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error in POST /tasks: {ex.Message}", ex);
            return CreateErrorResponse(request.RequestId, 500, $"Error creating task: {ex.Message}");
        }
    }

    private async Task<IPCResponse> HandleUpdateTaskAsync(IPCRequest request, string? resourceId)
    {
        try
        {
            if (string.IsNullOrEmpty(resourceId))
            {
                return CreateErrorResponse(request.RequestId, 400, "Task ID is required");
            }

            if (!Guid.TryParse(resourceId, out var taskId))
            {
                return CreateErrorResponse(request.RequestId, 400, "Invalid task ID format");
            }

            if (request.Body == null)
            {
                return CreateErrorResponse(request.RequestId, 400, "Request body is required");
            }

            // Get existing task
            var existingTask = await _taskManager.GetTaskByIdAsync(taskId);
            if (existingTask == null)
            {
                return CreateErrorResponse(request.RequestId, 404, "Task not found");
            }

            // Update task with new values
            var updatedTask = DictToTask(request.Body);
            updatedTask.Id = taskId;
            updatedTask.CreatedAt = existingTask.CreatedAt;

            var result = await _taskManager.UpdateTaskAsync(updatedTask);

            return CreateSuccessResponse(request.RequestId, TaskToDict(result));
        }
        catch (Exception ex)
        {
            _logger.Error($"Error in PUT /tasks: {ex.Message}", ex);
            return CreateErrorResponse(request.RequestId, 500, $"Error updating task: {ex.Message}");
        }
    }

    private async Task<IPCResponse> HandleDeleteTaskAsync(IPCRequest request, string? resourceId)
    {
        try
        {
            if (string.IsNullOrEmpty(resourceId))
            {
                return CreateErrorResponse(request.RequestId, 400, "Task ID is required");
            }

            if (!Guid.TryParse(resourceId, out var taskId))
            {
                return CreateErrorResponse(request.RequestId, 400, "Invalid task ID format");
            }

            await _taskManager.DeleteTaskAsync(taskId);

            return CreateSuccessResponse(request.RequestId, new Dictionary<string, object>
            {
                ["message"] = "Task deleted successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"Error in DELETE /tasks: {ex.Message}", ex);
            return CreateErrorResponse(request.RequestId, 500, $"Error deleting task: {ex.Message}");
        }
    }

    private async Task<IPCResponse> HandleTagsRequestAsync(IPCRequest request, string? resourceId)
    {
        return request.Method.ToUpperInvariant() switch
        {
            "GET" => await HandleGetTagsAsync(request, resourceId),
            "POST" => await HandleCreateTagAsync(request),
            "DELETE" => await HandleDeleteTagAsync(request, resourceId),
            _ => CreateErrorResponse(request.RequestId, 405, $"Method {request.Method} not allowed for /tags")
        };
    }

    private async Task<IPCResponse> HandleGetTagsAsync(IPCRequest request, string? resourceId)
    {
        try
        {
            // GET /tags/{taskId} - Get tags for specific task
            if (!string.IsNullOrEmpty(resourceId))
            {
                if (!Guid.TryParse(resourceId, out var taskId))
                {
                    return CreateErrorResponse(request.RequestId, 400, "Invalid task ID format");
                }

                var tags = await _tagManager.GetTagsForTaskAsync(taskId);
                return CreateSuccessResponse(request.RequestId, new Dictionary<string, object>
                {
                    ["tags"] = tags.Select(t => new Dictionary<string, object>
                    {
                        ["name"] = t.Name,
                        ["createdAt"] = t.CreatedAt
                    }).ToList()
                });
            }

            // GET /tags - Get all tags
            var allTags = await _tagManager.GetAllTagsAsync();
            return CreateSuccessResponse(request.RequestId, new Dictionary<string, object>
            {
                ["tags"] = allTags.Select(t => new Dictionary<string, object>
                {
                    ["name"] = t.Name,
                    ["createdAt"] = t.CreatedAt
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"Error in GET /tags: {ex.Message}", ex);
            return CreateErrorResponse(request.RequestId, 500, $"Error retrieving tags: {ex.Message}");
        }
    }

    private async Task<IPCResponse> HandleCreateTagAsync(IPCRequest request)
    {
        try
        {
            if (request.Body == null || !request.Body.TryGetValue("name", out var nameObj))
            {
                return CreateErrorResponse(request.RequestId, 400, "Tag name is required");
            }

            var tagName = nameObj.ToString()!;
            var tag = await _tagManager.CreateTagAsync(tagName);

            return CreateSuccessResponse(request.RequestId, new Dictionary<string, object>
            {
                ["name"] = tag.Name,
                ["createdAt"] = tag.CreatedAt
            }, 201);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error in POST /tags: {ex.Message}", ex);
            return CreateErrorResponse(request.RequestId, 500, $"Error creating tag: {ex.Message}");
        }
    }

    private async Task<IPCResponse> HandleDeleteTagAsync(IPCRequest request, string? resourceId)
    {
        try
        {
            if (string.IsNullOrEmpty(resourceId))
            {
                return CreateErrorResponse(request.RequestId, 400, "Tag name is required");
            }

            await _tagManager.DeleteTagAsync(resourceId);

            return CreateSuccessResponse(request.RequestId, new Dictionary<string, object>
            {
                ["message"] = "Tag deleted successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"Error in DELETE /tags: {ex.Message}", ex);
            return CreateErrorResponse(request.RequestId, 500, $"Error deleting tag: {ex.Message}");
        }
    }

    private async Task<IPCResponse> HandleTimeTrackingRequestAsync(IPCRequest request, string? resourceId)
    {
        return request.Method.ToUpperInvariant() switch
        {
            "GET" => await HandleGetTimeTrackingAsync(request, resourceId),
            "POST" => await HandleTimeTrackingActionAsync(request),
            _ => CreateErrorResponse(request.RequestId, 405, $"Method {request.Method} not allowed for /time-tracking")
        };
    }

    private async Task<IPCResponse> HandleGetTimeTrackingAsync(IPCRequest request, string? resourceId)
    {
        try
        {
            if (string.IsNullOrEmpty(resourceId))
            {
                return CreateErrorResponse(request.RequestId, 400, "Task ID is required");
            }

            if (!Guid.TryParse(resourceId, out var taskId))
            {
                return CreateErrorResponse(request.RequestId, 400, "Invalid task ID format");
            }

            var currentSession = _timeTracker.GetCurrentSession(taskId);
            var totalDuration = await _timeTracker.GetTotalTaskDurationAsync(taskId);
            var totalBreakTime = await _timeTracker.GetTotalBreakTimeAsync(taskId);

            var response = new Dictionary<string, object>
            {
                ["taskId"] = taskId,
                ["totalDuration"] = totalDuration.ToString(),
                ["totalBreakTime"] = totalBreakTime.ToString(),
                ["hasActiveSession"] = currentSession != null
            };

            if (currentSession != null)
            {
                response["currentSession"] = new Dictionary<string, object>
                {
                    ["id"] = currentSession.Id,
                    ["startTime"] = currentSession.StartTime,
                    ["elapsedTime"] = _timeTracker.GetElapsedTime(taskId).ToString(),
                    ["breakTime"] = _timeTracker.GetBreakTime(taskId).ToString()
                };
            }

            return CreateSuccessResponse(request.RequestId, response);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error in GET /time-tracking: {ex.Message}", ex);
            return CreateErrorResponse(request.RequestId, 500, $"Error retrieving time tracking data: {ex.Message}");
        }
    }

    private async Task<IPCResponse> HandleTimeTrackingActionAsync(IPCRequest request)
    {
        try
        {
            if (request.Body == null)
            {
                return CreateErrorResponse(request.RequestId, 400, "Request body is required");
            }

            if (!request.Body.TryGetValue("action", out var actionObj))
            {
                return CreateErrorResponse(request.RequestId, 400, "Action is required");
            }

            if (!request.Body.TryGetValue("taskId", out var taskIdObj))
            {
                return CreateErrorResponse(request.RequestId, 400, "Task ID is required");
            }

            if (!Guid.TryParse(taskIdObj.ToString(), out var taskId))
            {
                return CreateErrorResponse(request.RequestId, 400, "Invalid task ID format");
            }

            var action = actionObj.ToString()!.ToLowerInvariant();

            switch (action)
            {
                case "start":
                    await _timeTracker.StartTrackingAsync(taskId);
                    return CreateSuccessResponse(request.RequestId, new Dictionary<string, object>
                    {
                        ["message"] = "Time tracking started"
                    });

                case "stop":
                    await _timeTracker.StopTrackingAsync(taskId);
                    return CreateSuccessResponse(request.RequestId, new Dictionary<string, object>
                    {
                        ["message"] = "Time tracking stopped"
                    });

                case "start-break":
                    await _timeTracker.StartBreakAsync(taskId);
                    return CreateSuccessResponse(request.RequestId, new Dictionary<string, object>
                    {
                        ["message"] = "Break started"
                    });

                case "end-break":
                    await _timeTracker.EndBreakAsync(taskId);
                    return CreateSuccessResponse(request.RequestId, new Dictionary<string, object>
                    {
                        ["message"] = "Break ended"
                    });

                default:
                    return CreateErrorResponse(request.RequestId, 400, $"Unknown action: {action}");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error in POST /time-tracking: {ex.Message}", ex);
            return CreateErrorResponse(request.RequestId, 500, $"Error performing time tracking action: {ex.Message}");
        }
    }

    private Dictionary<string, object> TaskToDict(TaskEntity task)
    {
        var dict = new Dictionary<string, object>
        {
            ["id"] = task.Id,
            ["description"] = task.Description,
            ["taskTypeId"] = task.TaskTypeId,
            ["urgencyLevel"] = task.UrgencyLevel,
            ["createdAt"] = task.CreatedAt,
            ["updatedAt"] = task.UpdatedAt,
            ["tags"] = task.Tags
        };

        if (task.DeadlineDate.HasValue)
            dict["deadlineDate"] = task.DeadlineDate.Value;

        if (task.DeadlineTime.HasValue)
            dict["deadlineTime"] = task.DeadlineTime.Value.ToString();

        if (task.Importance.HasValue)
            dict["importance"] = task.Importance.Value;

        if (task.Complexity.HasValue)
            dict["complexity"] = task.Complexity.Value;

        return dict;
    }

    private TaskEntity DictToTask(Dictionary<string, object> dict)
    {
        var task = new TaskEntity
        {
            Id = dict.ContainsKey("id") ? Guid.Parse(dict["id"].ToString()!) : Guid.NewGuid(),
            Description = dict.ContainsKey("description") ? dict["description"].ToString()! : string.Empty,
            TaskTypeId = dict.ContainsKey("taskTypeId") ? Convert.ToInt32(dict["taskTypeId"]) : 1,
            UrgencyLevel = dict.ContainsKey("urgencyLevel") ? Convert.ToInt32(dict["urgencyLevel"]) : 0,
            CreatedAt = dict.ContainsKey("createdAt") ? DateTime.Parse(dict["createdAt"].ToString()!) : DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (dict.ContainsKey("deadlineDate"))
            task.DeadlineDate = DateTime.Parse(dict["deadlineDate"].ToString()!);

        if (dict.ContainsKey("deadlineTime"))
            task.DeadlineTime = TimeSpan.Parse(dict["deadlineTime"].ToString()!);

        if (dict.ContainsKey("importance"))
            task.Importance = Convert.ToDouble(dict["importance"]);

        if (dict.ContainsKey("complexity"))
            task.Complexity = Convert.ToDouble(dict["complexity"]);

        if (dict.ContainsKey("tags"))
            task.Tags = ParseStringArray(dict["tags"]);

        return task;
    }

    private List<string> ParseStringArray(object obj)
    {
        if (obj is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
        {
            return jsonElement.EnumerateArray()
                .Select(e => e.GetString() ?? string.Empty)
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }

        if (obj is IEnumerable<object> enumerable)
        {
            return enumerable.Select(o => o.ToString() ?? string.Empty)
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }

        return new List<string>();
    }

    private IPCResponse CreateSuccessResponse(string requestId, Dictionary<string, object> body, int statusCode = 200)
    {
        return new IPCResponse
        {
            RequestId = requestId,
            StatusCode = statusCode,
            Body = body
        };
    }

    private IPCResponse CreateErrorResponse(string requestId, int statusCode, string error)
    {
        return new IPCResponse
        {
            RequestId = requestId,
            StatusCode = statusCode,
            Error = error
        };
    }
}
