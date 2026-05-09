using DayPlannerApp.Models;
using DayPlannerApp.Repositories;
using FsCheck;
using FsCheck.Xunit;

namespace DayPlannerApp.Tests;

public class TaskRepositoryPropertyTests
{
    // Feature: day-planner-app, Property 1: Task CRUD Round-Trip
    // For any valid task data, creating a task and then retrieving it by ID should return a task with equivalent data to what was provided at creation.
    // Validates: Requirements 1.1, 1.3
    [Property(MaxTest = 100)]
    public Property TaskCRUDRoundTrip_PreservesAllFields()
    {
        return Prop.ForAll(
            TaskEntityGenerator(),
            task =>
            {
                using var dbHelper = new TestDatabaseHelper();
                var repository = new TaskRepository(dbHelper.ConnectionString);

                var created = repository.InsertAsync(task).GetAwaiter().GetResult();
                var retrieved = repository.GetByIdAsync(created.Id).GetAwaiter().GetResult();

                return retrieved != null &&
                       retrieved.Id == task.Id &&
                       retrieved.Description == task.Description &&
                       retrieved.TaskTypeId == task.TaskTypeId &&
                       retrieved.DeadlineDate == task.DeadlineDate &&
                       retrieved.DeadlineTime == task.DeadlineTime &&
                       retrieved.Importance == task.Importance &&
                       retrieved.Complexity == task.Complexity &&
                       retrieved.UrgencyLevel == task.UrgencyLevel &&
                       retrieved.CreatedAt == task.CreatedAt &&
                       retrieved.UpdatedAt == task.UpdatedAt &&
                       TagsEqual(retrieved.Tags, task.Tags);
            });
    }

    // Feature: day-planner-app, Property 2: Task Update Preservation
    // For any existing task and any valid update data, updating the task and then retrieving it should return a task with the updated fields changed and all other fields unchanged.
    // Validates: Requirements 1.2
    [Property(MaxTest = 100)]
    public Property TaskUpdatePreservation_UpdatesOnlyModifiedFields()
    {
        return Prop.ForAll(
            TaskEntityGenerator(),
            TaskEntityGenerator(),
            (originalTask, updateData) =>
            {
                using var dbHelper = new TestDatabaseHelper();
                var repository = new TaskRepository(dbHelper.ConnectionString);

                // Insert original task
                var created = repository.InsertAsync(originalTask).GetAwaiter().GetResult();

                // Update with new data but keep original ID and CreatedAt
                var updatedTask = new TaskEntity
                {
                    Id = created.Id,
                    Description = updateData.Description,
                    TaskTypeId = updateData.TaskTypeId,
                    DeadlineDate = updateData.DeadlineDate,
                    DeadlineTime = updateData.DeadlineTime,
                    Importance = updateData.Importance,
                    Complexity = updateData.Complexity,
                    UrgencyLevel = updateData.UrgencyLevel,
                    CreatedAt = created.CreatedAt,
                    UpdatedAt = updateData.UpdatedAt,
                    Tags = updateData.Tags
                };

                repository.UpdateAsync(updatedTask).GetAwaiter().GetResult();
                var retrieved = repository.GetByIdAsync(updatedTask.Id).GetAwaiter().GetResult();

                return retrieved != null &&
                       retrieved.Id == updatedTask.Id &&
                       retrieved.Description == updatedTask.Description &&
                       retrieved.TaskTypeId == updatedTask.TaskTypeId &&
                       retrieved.DeadlineDate == updatedTask.DeadlineDate &&
                       retrieved.DeadlineTime == updatedTask.DeadlineTime &&
                       retrieved.Importance == updatedTask.Importance &&
                       retrieved.Complexity == updatedTask.Complexity &&
                       retrieved.UrgencyLevel == updatedTask.UrgencyLevel &&
                       retrieved.CreatedAt == updatedTask.CreatedAt &&
                       retrieved.UpdatedAt == updatedTask.UpdatedAt &&
                       TagsEqual(retrieved.Tags, updatedTask.Tags);
            });
    }

    private static Arbitrary<TaskEntity> TaskEntityGenerator()
    {
        var gen = from description in Arb.Generate<NonEmptyString>()
                  from taskTypeId in Gen.Choose(1, 4)
                  from deadlineDate in Gen.OneOf(
                      Gen.Constant<DateTime?>(null),
                      Arb.Generate<DateTime>().Select(d => (DateTime?)d.Date))
                  from deadlineTime in Gen.OneOf(
                      Gen.Constant<TimeSpan?>(null),
                      Gen.Choose(0, 86399).Select(s => (TimeSpan?)TimeSpan.FromSeconds(s)))
                  from importance in Gen.OneOf(
                      Gen.Constant<double?>(null),
                      Gen.Choose(0, 100).Select(i => (double?)i))
                  from complexity in Gen.OneOf(
                      Gen.Constant<double?>(null),
                      Gen.Choose(0, 100).Select(i => (double?)i))
                  from urgencyLevel in Gen.Choose(0, 10)
                  from createdAt in Arb.Generate<DateTime>()
                  from updatedAt in Arb.Generate<DateTime>()
                  from tags in Gen.ListOf(Arb.Generate<NonEmptyString>().Select(s => s.Get))
                  select new TaskEntity
                  {
                      Id = Guid.NewGuid(),
                      Description = description.Get,
                      TaskTypeId = taskTypeId,
                      DeadlineDate = deadlineDate,
                      DeadlineTime = deadlineTime,
                      Importance = importance,
                      Complexity = complexity,
                      UrgencyLevel = urgencyLevel,
                      CreatedAt = createdAt,
                      UpdatedAt = updatedAt,
                      Tags = tags.Distinct().ToList()
                  };

        return Arb.From(gen);
    }

    private static bool TagsEqual(List<string> tags1, List<string> tags2)
    {
        if (tags1.Count != tags2.Count) return false;
        var sorted1 = tags1.OrderBy(t => t).ToList();
        var sorted2 = tags2.OrderBy(t => t).ToList();
        return sorted1.SequenceEqual(sorted2);
    }
}
