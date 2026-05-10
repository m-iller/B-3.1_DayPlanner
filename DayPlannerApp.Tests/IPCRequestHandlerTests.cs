using Xunit;
using DayPlannerApp.Services;
using DayPlannerApp.Models;
using DayPlannerApp.Repositories;

namespace DayPlannerApp.Tests;

/// <summary>
/// Unit tests for IPC request handler routing and business logic integration.
/// Validates request routing to TaskManager, TagManager, and TimeTracker services.
/// </summary>
public class IPCRequestHandlerTests : IDisposable
{
    private readonly TestDatabaseHelper _dbHelper;
    private readonly ITaskRepository _taskRepository;
    private readonly ITagRepository _tagRepository;
    private readonly ITimeTrackingRepository _timeTrackingRepository;
    private readonly ITaskManager _taskManager;
    private readonly ITagManager _tagManager;
    private readonly ITimeTracker _timeTracker;
    private readonly IPCRequestHandler _requestHandler;
    private readonly TestLogger _logger;

    public IPCRequestHandlerTests()
    {
        _dbHelper = new TestDatabaseHelper();
        _logger = new TestLogger();
        
        // Setup repositories
        _taskRepository = new TaskRepository(_dbHelper.ConnectionString);
        _tagRepository = new TagRepository(_dbHelper.ConnectionString);
        _timeTrackingRepository = new TimeTrackingRepository(_dbHelper.ConnectionString);
        
        // Setup services
        _taskManager = new TaskManager(_taskRepository, _logger);
        _tagManager = new TagManager(_tagRepository, _taskRepository);
        _timeTracker = new TimeTracker(_timeTrackingRepository, _logger);
        
        // Setup request handler
        _requestHandler = new IPCRequestHandler(_taskManager, _tagManager, _timeTracker, _logger);
    }

    #region Task Resource Tests

    [Fact]
    public async Task HandleRequest_CreateTask_ReturnsCreatedTask()
    {
        // Arrange
        var request = new IPCRequest
        {
            RequestId = Guid.NewGuid().ToString(),
            Method = "POST",
            Resource = "/tasks",
            Body = new Dictionary<string, object>
            {
                ["description"] = "Test task",
                ["taskTypeId"] = 1,
                ["urgencyLevel"] = 0
            }
        };

        // Act
        var response = await _requestHandler.HandleRequestAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(request.RequestId, response.RequestId);
        Assert.Equal(200, response.StatusCode);
        Assert.NotNull(response.Body);
        Assert.True(response.Body.ContainsKey("task"));
    }

    [Fact]
    public async Task HandleRequest_GetTaskById_ReturnsTask()
    {
        // Arrange - Create a task first
        var task = new TaskEntity
        {
            Id = Guid.NewGuid(),
            Description = "Test task",
            TaskTypeId = 1,
            UrgencyLevel = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _taskRepository.InsertAsync(task);

        var request = new IPCRequest
        {
            RequestId = Guid.NewGuid().ToString(),
            Method = "GET",
            Resource = "/tasks",
            Body = new Dictionary<string, object>
            {
                ["id"] = task.Id.ToString()
            }
        };

        // Act
        var response = await _requestHandler.HandleRequestAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(200, response.StatusCode);
        Assert.NotNull(response.Body);
        Assert.True(response.Body.ContainsKey("task"));
    }

    [Fact]
    public async Task HandleRequest_GetTaskById_NotFound_Returns404()
    {
        // Arrange
        var request = new IPCRequest
        {
            RequestId = Guid.NewGuid().ToString(),
            Method = "GET",
            Resource = "/tasks",
            Body = new Dictionary<string, object>
            {
                ["id"] = Guid.NewGuid().ToString()
            }
        };

        // Act
        var response = await _requestHandler.HandleRequestAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(404, response.StatusCode);
        Assert.Contains("not found", response.Error ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HandleRequest_UpdateTask_ReturnsUpdatedTask()
    {
        // Arrange - Create a task first
        var task = new TaskEntity
        {
            Id = Guid.NewGuid(),
            Description = "Original description",
            TaskTypeId = 1,
            UrgencyLevel = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _taskRepository.InsertAsync(task);

        var request = new IPCRequest
        {
            RequestId = Guid.NewGuid().ToString(),
            Method = "PUT",
            Resource = "/tasks",
            Body = new Dictionary<string, object>
            {
                ["id"] = task.Id.ToString(),
                ["description"] = "Updated description",
                ["taskTypeId"] = 1,
                ["urgencyLevel"] = 0
            }
        };

        // Act
        var response = await _requestHandler.HandleRequestAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(200, response.StatusCode);
        Assert.NotNull(response.Body);
        Assert.True(response.Body.ContainsKey("task"));
    }

    [Fact]
    public async Task HandleRequest_DeleteTask_ReturnsSuccess()
    {
        // Arrange - Create a task first
        var task = new TaskEntity
        {
            Id = Guid.NewGuid(),
            Description = "Task to delete",
            TaskTypeId = 1,
            UrgencyLevel = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _taskRepository.InsertAsync(task);

        var request = new IPCRequest
        {
            RequestId = Guid.NewGuid().ToString(),
            Method = "DELETE",
            Resource = "/tasks",
            Body = new Dictionary<string, object>
            {
                ["id"] = task.Id.ToString()
            }
        };

        // Act
        var response = await _requestHandler.HandleRequestAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(200, response.StatusCode);
        Assert.NotNull(response.Body);
        Assert.True(response.Body.ContainsKey("message"));
    }

    [Fact]
    public async Task HandleRequest_GetTasksByTags_ReturnsTasks()
    {
        // Arrange - Create task with tags
        var task = new TaskEntity
        {
            Id = Guid.NewGuid(),
            Description = "Tagged task",
            TaskTypeId = 1,
            UrgencyLevel = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _taskRepository.InsertAsync(task);
        await _tagRepository.CreateTagAsync("urgent");
        await _tagRepository.AssignTagToTaskAsync(task.Id, "urgent");

        var request = new IPCRequest
        {
            RequestId = Guid.NewGuid().ToString(),
            Method = "GET",
            Resource = "/tasks",
            Body = new Dictionary<string, object>
            {
                ["tags"] = new[] { "urgent" }
            }
        };

        // Act
        var response = await _requestHandler.HandleRequestAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(200, response.StatusCode);
        Assert.NotNull(response.Body);
        Assert.True(response.Body.ContainsKey("tasks"));
    }

    [Fact]
    public async Task HandleRequest_GetTasksByType_ReturnsTasks()
    {
        // Arrange - Create task
        var task = new TaskEntity
        {
            Id = Guid.NewGuid(),
            Description = "Work task",
            TaskTypeId = 3,
            UrgencyLevel = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _taskRepository.InsertAsync(task);

        var request = new IPCRequest
        {
            RequestId = Guid.NewGuid().ToString(),
            Method = "GET",
            Resource = "/tasks",
            Body = new Dictionary<string, object>
            {
                ["typeId"] = 3
            }
        };

        // Act
        var response = await _requestHandler.HandleRequestAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(200, response.StatusCode);
        Assert.NotNull(response.Body);
        Assert.True(response.Body.ContainsKey("tasks"));
    }

    [Fact]
    public async Task HandleRequest_GetTasksByDateRange_ReturnsTasks()
    {
        // Arrange - Create task with deadline
        var task = new TaskEntity
        {
            Id = Guid.NewGuid(),
            Description = "Task with deadline",
            TaskTypeId = 1,
            DeadlineDate = DateTime.Today.AddDays(5),
            UrgencyLevel = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _taskRepository.InsertAsync(task);

        var request = new IPCRequest
        {
            RequestId = Guid.NewGuid().ToString(),
            Method = "GET",
            Resource = "/tasks",
            Body = new Dictionary<string, object>
            {
                ["startDate"] = DateTime.Today.ToString("o"),
                ["endDate"] = DateTime.Today.AddDays(10).ToString("o")
            }
        };

        // Act
        var response = await _requestHandler.HandleRequestAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(200, response.StatusCode);
        Assert.NotNull(response.Body);
        Assert.True(response.Body.ContainsKey("tasks"));
    }

    #endregion

    #region Tag Resource Tests

    [Fact]
    public async Task HandleRequest_GetAllTags_ReturnsTags()
    {
        // Arrange - Create some tags
        await _tagRepository.CreateTagAsync("urgent");
        await _tagRepository.CreateTagAsync("important");

        var request = new IPCRequest
        {
            RequestId = Guid.NewGuid().ToString(),
            Method = "GET",
            Resource = "/tags",
            Body = null
        };

        // Act
        var response = await _requestHandler.HandleRequestAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(200, response.StatusCode);
        Assert.NotNull(response.Body);
        Assert.True(response.Body.ContainsKey("tags"));
    }

    [Fact]
    public async Task HandleRequest_CreateTag_ReturnsCreatedTag()
    {
        // Arrange
        var request = new IPCRequest
        {
            RequestId = Guid.NewGuid().ToString(),
            Method = "POST",
            Resource = "/tags",
            Body = new Dictionary<string, object>
            {
                ["name"] = "new-tag"
            }
        };

        // Act
        var response = await _requestHandler.HandleRequestAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(200, response.StatusCode);
        Assert.NotNull(response.Body);
        Assert.True(response.Body.ContainsKey("tag"));
    }

    [Fact]
    public async Task HandleRequest_AssignTagToTask_ReturnsSuccess()
    {
        // Arrange - Create task and tag
        var task = new TaskEntity
        {
            Id = Guid.NewGuid(),
            Description = "Task for tagging",
            TaskTypeId = 1,
            UrgencyLevel = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _taskRepository.InsertAsync(task);
        await _tagRepository.CreateTagAsync("test-tag");

        var request = new IPCRequest
        {
            RequestId = Guid.NewGuid().ToString(),
            Method = "POST",
            Resource = "/tags",
            Body = new Dictionary<string, object>
            {
                ["name"] = "test-tag",
                ["taskId"] = task.Id.ToString()
            }
        };

        // Act
        var response = await _requestHandler.HandleRequestAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(200, response.StatusCode);
        Assert.NotNull(response.Body);
        Assert.True(response.Body.ContainsKey("message"));
    }

    [Fact]
    public async Task HandleRequest_DeleteTag_ReturnsSuccess()
    {
        // Arrange - Create tag
        await _tagRepository.CreateTagAsync("tag-to-delete");

        var request = new IPCRequest
        {
            RequestId = Guid.NewGuid().ToString(),
            Method = "DELETE",
            Resource = "/tags",
            Body = new Dictionary<string, object>
            {
                ["name"] = "tag-to-delete"
            }
        };

        // Act
        var response = await _requestHandler.HandleRequestAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(200, response.StatusCode);
        Assert.NotNull(response.Body);
        Assert.True(response.Body.ContainsKey("message"));
    }

    [Fact]
    public async Task HandleRequest_GetTagsForTask_ReturnsTags()
    {
        // Arrange - Create task with tags
        var task = new TaskEntity
        {
            Id = Guid.NewGuid(),
            Description = "Task with tags",
            TaskTypeId = 1,
            UrgencyLevel = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _taskRepository.InsertAsync(task);
        await _tagRepository.CreateTagAsync("tag1");
        await _tagRepository.AssignTagToTaskAsync(task.Id, "tag1");

        var request = new IPCRequest
        {
            RequestId = Guid.NewGuid().ToString(),
            Method = "GET",
            Resource = "/tags",
            Body = new Dictionary<string, object>
            {
                ["taskId"] = task.Id.ToString()
            }
        };

        // Act
        var response = await _requestHandler.HandleRequestAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(200, response.StatusCode);
        Assert.NotNull(response.Body);
        Assert.True(response.Body.ContainsKey("tags"));
    }

    #endregion

    #region Time Tracking Resource Tests

    [Fact]
    public async Task HandleRequest_StartTimeTracking_ReturnsSuccess()
    {
        // Arrange - Create task
        var task = new TaskEntity
        {
            Id = Guid.NewGuid(),
            Description = "Task for time tracking",
            TaskTypeId = 1,
            UrgencyLevel = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _taskRepository.InsertAsync(task);

        var request = new IPCRequest
        {
            RequestId = Guid.NewGuid().ToString(),
            Method = "POST",
            Resource = "/time-tracking",
            Body = new Dictionary<string, object>
            {
                ["action"] = "start",
                ["taskId"] = task.Id.ToString()
            }
        };

        // Act
        var response = await _requestHandler.HandleRequestAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(200, response.StatusCode);
        Assert.NotNull(response.Body);
        Assert.True(response.Body.ContainsKey("message"));
    }

    [Fact]
    public async Task HandleRequest_StopTimeTracking_ReturnsSuccess()
    {
        // Arrange - Create task and start tracking
        var task = new TaskEntity
        {
            Id = Guid.NewGuid(),
            Description = "Task for time tracking",
            TaskTypeId = 1,
            UrgencyLevel = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _taskRepository.InsertAsync(task);
        await _timeTracker.StartTrackingAsync(task.Id);

        var request = new IPCRequest
        {
            RequestId = Guid.NewGuid().ToString(),
            Method = "POST",
            Resource = "/time-tracking",
            Body = new Dictionary<string, object>
            {
                ["action"] = "stop",
                ["taskId"] = task.Id.ToString()
            }
        };

        // Act
        var response = await _requestHandler.HandleRequestAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(200, response.StatusCode);
        Assert.NotNull(response.Body);
        Assert.True(response.Body.ContainsKey("message"));
    }

    [Fact]
    public async Task HandleRequest_GetTimeTrackingInfo_ReturnsInfo()
    {
        // Arrange - Create task
        var task = new TaskEntity
        {
            Id = Guid.NewGuid(),
            Description = "Task for time tracking",
            TaskTypeId = 1,
            UrgencyLevel = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _taskRepository.InsertAsync(task);

        var request = new IPCRequest
        {
            RequestId = Guid.NewGuid().ToString(),
            Method = "GET",
            Resource = "/time-tracking",
            Body = new Dictionary<string, object>
            {
                ["taskId"] = task.Id.ToString()
            }
        };

        // Act
        var response = await _requestHandler.HandleRequestAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(200, response.StatusCode);
        Assert.NotNull(response.Body);
        Assert.True(response.Body.ContainsKey("elapsedTime"));
        Assert.True(response.Body.ContainsKey("breakTime"));
        Assert.True(response.Body.ContainsKey("totalDuration"));
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task HandleRequest_InvalidResource_Returns404()
    {
        // Arrange
        var request = new IPCRequest
        {
            RequestId = Guid.NewGuid().ToString(),
            Method = "GET",
            Resource = "/invalid-resource",
            Body = null
        };

        // Act
        var response = await _requestHandler.HandleRequestAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(404, response.StatusCode);
        Assert.Contains("not found", response.Error ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HandleRequest_InvalidMethod_Returns400()
    {
        // Arrange
        var request = new IPCRequest
        {
            RequestId = Guid.NewGuid().ToString(),
            Method = "PATCH",
            Resource = "/tasks",
            Body = null
        };

        // Act
        var response = await _requestHandler.HandleRequestAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(400, response.StatusCode);
        Assert.Contains("not supported", response.Error ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HandleRequest_NullRequest_Returns400()
    {
        // Act
        var response = await _requestHandler.HandleRequestAsync(null!);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(400, response.StatusCode);
        Assert.Contains("cannot be null", response.Error ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HandleRequest_MissingRequiredField_Returns400()
    {
        // Arrange - POST without body
        var request = new IPCRequest
        {
            RequestId = Guid.NewGuid().ToString(),
            Method = "POST",
            Resource = "/tasks",
            Body = null
        };

        // Act
        var response = await _requestHandler.HandleRequestAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(400, response.StatusCode);
        Assert.Contains("required", response.Error ?? "", StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    public void Dispose()
    {
        _dbHelper?.Dispose();
    }

    private class TestLogger : ILogger
    {
        public void Debug(string message) { }
        public void Info(string message) { }
        public void Warning(string message) { }
        public void Error(string message, Exception? exception = null) { }
        public void Fatal(string message, Exception? exception = null) { }
    }
}
