using DayPlannerApp.Models;
using DayPlannerApp.Repositories;
using DayPlannerApp.Services;

namespace DayPlannerApp.Tests;

public class TaskManagerTests
{
    [Fact]
    public async Task CreateTask_ValidatesCoordinateRanges()
    {
        using var dbHelper = new TestDatabaseHelper();
        var repository = new TaskRepository(dbHelper.ConnectionString);
        var manager = new TaskManager(repository);

        var task = new TaskEntity
        {
            Id = Guid.NewGuid(),
            Description = "Test",
            TaskTypeId = 1,
            Importance = 150.0, // Invalid: > 100
            Complexity = 50.0,
            UrgencyLevel = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Tags = new List<string>()
        };

        await Assert.ThrowsAsync<ArgumentException>(async () => 
            await manager.CreateTaskAsync(task));
    }

    [Fact]
    public async Task GetTasksByDateRange_ReturnsTasksInRange()
    {
        using var dbHelper = new TestDatabaseHelper();
        var repository = new TaskRepository(dbHelper.ConnectionString);
        var manager = new TaskManager(repository);

        var task1 = new TaskEntity
        {
            Id = Guid.NewGuid(),
            Description = "Task 1",
            TaskTypeId = 1,
            DeadlineDate = DateTime.Today.AddDays(5),
            UrgencyLevel = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Tags = new List<string>()
        };

        var task2 = new TaskEntity
        {
            Id = Guid.NewGuid(),
            Description = "Task 2",
            TaskTypeId = 1,
            DeadlineDate = DateTime.Today.AddDays(15),
            UrgencyLevel = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Tags = new List<string>()
        };

        await manager.CreateTaskAsync(task1);
        await manager.CreateTaskAsync(task2);

        var tasks = await manager.GetTasksByDateRangeAsync(
            DateTime.Today, 
            DateTime.Today.AddDays(10));

        Assert.Single(tasks);
        Assert.Equal("Task 1", tasks.First().Description);
    }
}
