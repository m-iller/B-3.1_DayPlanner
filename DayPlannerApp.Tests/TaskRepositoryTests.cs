using DayPlannerApp.Models;
using DayPlannerApp.Repositories;

namespace DayPlannerApp.Tests;

public class TaskRepositoryTests
{
    [Fact]
    public async Task TaskCRUDRoundTrip_CreatesAndRetrievesTask()
    {
        using var dbHelper = new TestDatabaseHelper();
        var repository = new TaskRepository(dbHelper.ConnectionString);
        
        var task = new TaskEntity
        {
            Id = Guid.NewGuid(),
            Description = "Test task",
            TaskTypeId = 1,
            UrgencyLevel = 5,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Tags = new List<string>()
        };

        var createdTask = await repository.InsertAsync(task);
        var retrievedTask = await repository.GetByIdAsync(createdTask.Id);

        Assert.NotNull(retrievedTask);
        Assert.Equal(task.Id, retrievedTask.Id);
        Assert.Equal(task.Description, retrievedTask.Description);
    }

    [Fact]
    public async Task TaskUpdate_UpdatesFields()
    {
        using var dbHelper = new TestDatabaseHelper();
        var repository = new TaskRepository(dbHelper.ConnectionString);
        
        var task = new TaskEntity
        {
            Id = Guid.NewGuid(),
            Description = "Original",
            TaskTypeId = 1,
            UrgencyLevel = 3,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Tags = new List<string>()
        };

        var created = await repository.InsertAsync(task);
        created.Description = "Updated";
        await repository.UpdateAsync(created);
        var retrieved = await repository.GetByIdAsync(created.Id);

        Assert.Equal("Updated", retrieved!.Description);
    }
}
