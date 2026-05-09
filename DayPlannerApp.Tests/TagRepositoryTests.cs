using DayPlannerApp.Models;
using DayPlannerApp.Repositories;

namespace DayPlannerApp.Tests;

public class TagRepositoryTests
{
    [Fact]
    public async Task CreateTag_StoresAndRetrievesTag()
    {
        using var dbHelper = new TestDatabaseHelper();
        var repository = new TagRepository(dbHelper.ConnectionString);

        await repository.CreateTagAsync("urgent");

        var tags = await repository.GetAllTagsAsync();
        Assert.Contains(tags, t => t.Name == "urgent");
    }

    [Fact]
    public async Task AssignTagToTask_CreatesAssociation()
    {
        using var dbHelper = new TestDatabaseHelper();
        var tagRepo = new TagRepository(dbHelper.ConnectionString);
        var taskRepo = new TaskRepository(dbHelper.ConnectionString);

        var task = new TaskEntity
        {
            Id = Guid.NewGuid(),
            Description = "Test",
            TaskTypeId = 1,
            UrgencyLevel = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Tags = new List<string>()
        };
        await taskRepo.InsertAsync(task);

        await tagRepo.CreateTagAsync("work");
        await tagRepo.AssignTagToTaskAsync(task.Id, "work");

        var taskTags = await tagRepo.GetTagsForTaskAsync(task.Id);
        Assert.Contains(taskTags, t => t.Name == "work");
    }

    [Fact]
    public async Task RemoveTagFromTask_DeletesAssociation()
    {
        using var dbHelper = new TestDatabaseHelper();
        var tagRepo = new TagRepository(dbHelper.ConnectionString);
        var taskRepo = new TaskRepository(dbHelper.ConnectionString);

        var task = new TaskEntity
        {
            Id = Guid.NewGuid(),
            Description = "Test",
            TaskTypeId = 1,
            UrgencyLevel = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Tags = new List<string>()
        };
        await taskRepo.InsertAsync(task);

        await tagRepo.CreateTagAsync("urgent");
        await tagRepo.AssignTagToTaskAsync(task.Id, "urgent");
        await tagRepo.RemoveTagFromTaskAsync(task.Id, "urgent");

        var taskTags = await tagRepo.GetTagsForTaskAsync(task.Id);
        Assert.DoesNotContain(taskTags, t => t.Name == "urgent");
    }
}
