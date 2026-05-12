using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DayPlannerApp.Models;
using Microsoft.Data.Sqlite;

namespace DayPlannerApp.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly string _connectionString;

    public TaskRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public async Task<TaskEntity> InsertAsync(TaskEntity task)
    {
        if (task == null) throw new ArgumentNullException(nameof(task));

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();
        try
        {
            // Insert task
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
                    INSERT INTO Tasks (Id, Name, Description, TaskTypeId, DeadlineDate, DeadlineTime, 
                                       Importance, Complexity, UrgencyLevel, IsCompleted, CreatedAt, UpdatedAt)
                    VALUES (@Id, @Name, @Description, @TaskTypeId, @DeadlineDate, @DeadlineTime, 
                            @Importance, @Complexity, @UrgencyLevel, @IsCompleted, @CreatedAt, @UpdatedAt)";

                command.Parameters.AddWithValue("@Id", task.Id.ToString());
                command.Parameters.AddWithValue("@Name", task.Name);
                command.Parameters.AddWithValue("@Description", task.Description);
                command.Parameters.AddWithValue("@TaskTypeId", task.TaskTypeId);
                command.Parameters.AddWithValue("@DeadlineDate", task.DeadlineDate?.ToString("yyyy-MM-dd") ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@DeadlineTime", task.DeadlineTime?.ToString() ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Importance", task.Importance ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Complexity", task.Complexity ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@UrgencyLevel", task.UrgencyLevel);
                command.Parameters.AddWithValue("@IsCompleted", task.IsCompleted ? 1 : 0);
                command.Parameters.AddWithValue("@CreatedAt", task.CreatedAt.ToString("o"));
                command.Parameters.AddWithValue("@UpdatedAt", task.UpdatedAt.ToString("o"));

                await command.ExecuteNonQueryAsync();
            }

            // Insert tags
            if (task.Tags != null && task.Tags.Any())
            {
                foreach (var tag in task.Tags)
                {
                    // Ensure tag exists
                    using (var tagCommand = connection.CreateCommand())
                    {
                        tagCommand.Transaction = transaction;
                        tagCommand.CommandText = @"
                            INSERT OR IGNORE INTO Tags (Name, CreatedAt)
                            VALUES (@Name, @CreatedAt)";
                        tagCommand.Parameters.AddWithValue("@Name", tag);
                        tagCommand.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow.ToString("o"));
                        await tagCommand.ExecuteNonQueryAsync();
                    }

                    // Link task to tag
                    using (var linkCommand = connection.CreateCommand())
                    {
                        linkCommand.Transaction = transaction;
                        linkCommand.CommandText = @"
                            INSERT INTO TaskTags (TaskId, TagName)
                            VALUES (@TaskId, @TagName)";
                        linkCommand.Parameters.AddWithValue("@TaskId", task.Id.ToString());
                        linkCommand.Parameters.AddWithValue("@TagName", tag);
                        await linkCommand.ExecuteNonQueryAsync();
                    }
                }
            }

            transaction.Commit();
            return task;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<TaskEntity> UpdateAsync(TaskEntity task)
    {
        if (task == null) throw new ArgumentNullException(nameof(task));

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();
        try
        {
            // Update task
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
                    UPDATE Tasks 
                    SET Name = @Name,
                        Description = @Description,
                        TaskTypeId = @TaskTypeId,
                        DeadlineDate = @DeadlineDate,
                        DeadlineTime = @DeadlineTime,
                        Importance = @Importance,
                        Complexity = @Complexity,
                        UrgencyLevel = @UrgencyLevel,
                        IsCompleted = @IsCompleted,
                        UpdatedAt = @UpdatedAt
                    WHERE Id = @Id";

                command.Parameters.AddWithValue("@Id", task.Id.ToString());
                command.Parameters.AddWithValue("@Name", task.Name);
                command.Parameters.AddWithValue("@Description", task.Description);
                command.Parameters.AddWithValue("@TaskTypeId", task.TaskTypeId);
                command.Parameters.AddWithValue("@DeadlineDate", task.DeadlineDate?.ToString("yyyy-MM-dd") ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@DeadlineTime", task.DeadlineTime?.ToString() ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Importance", task.Importance ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Complexity", task.Complexity ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@UrgencyLevel", task.UrgencyLevel);
                command.Parameters.AddWithValue("@IsCompleted", task.IsCompleted ? 1 : 0);
                command.Parameters.AddWithValue("@UpdatedAt", task.UpdatedAt.ToString("o"));

                await command.ExecuteNonQueryAsync();
            }

            // Delete existing tag associations
            using (var deleteCommand = connection.CreateCommand())
            {
                deleteCommand.Transaction = transaction;
                deleteCommand.CommandText = "DELETE FROM TaskTags WHERE TaskId = @TaskId";
                deleteCommand.Parameters.AddWithValue("@TaskId", task.Id.ToString());
                await deleteCommand.ExecuteNonQueryAsync();
            }

            // Insert new tag associations
            if (task.Tags != null && task.Tags.Any())
            {
                foreach (var tag in task.Tags)
                {
                    // Ensure tag exists
                    using (var tagCommand = connection.CreateCommand())
                    {
                        tagCommand.Transaction = transaction;
                        tagCommand.CommandText = @"
                            INSERT OR IGNORE INTO Tags (Name, CreatedAt)
                            VALUES (@Name, @CreatedAt)";
                        tagCommand.Parameters.AddWithValue("@Name", tag);
                        tagCommand.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow.ToString("o"));
                        await tagCommand.ExecuteNonQueryAsync();
                    }

                    // Link task to tag
                    using (var linkCommand = connection.CreateCommand())
                    {
                        linkCommand.Transaction = transaction;
                        linkCommand.CommandText = @"
                            INSERT INTO TaskTags (TaskId, TagName)
                            VALUES (@TaskId, @TagName)";
                        linkCommand.Parameters.AddWithValue("@TaskId", task.Id.ToString());
                        linkCommand.Parameters.AddWithValue("@TagName", tag);
                        await linkCommand.ExecuteNonQueryAsync();
                    }
                }
            }

            transaction.Commit();
            return task;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task DeleteAsync(Guid taskId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Tasks WHERE Id = @Id";
        command.Parameters.AddWithValue("@Id", taskId.ToString());
        await command.ExecuteNonQueryAsync();
    }

    public async Task<TaskEntity?> GetByIdAsync(Guid taskId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        TaskEntity? task = null;

        // Get task
        using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
                SELECT t.Id, t.Name, t.Description, t.TaskTypeId, t.DeadlineDate, t.DeadlineTime, 
                       t.Importance, t.Complexity, t.UrgencyLevel, t.IsCompleted, t.CreatedAt, t.UpdatedAt,
                       tt.Name as TaskTypeName
                FROM Tasks t
                LEFT JOIN TaskTypes tt ON t.TaskTypeId = tt.Id
                WHERE t.Id = @Id";
            command.Parameters.AddWithValue("@Id", taskId.ToString());

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                task = MapTaskFromReader(reader);
            }
        }

        if (task == null) return null;

        // Get tags
        using (var tagCommand = connection.CreateCommand())
        {
            tagCommand.CommandText = @"
                SELECT TagName
                FROM TaskTags
                WHERE TaskId = @TaskId";
            tagCommand.Parameters.AddWithValue("@TaskId", taskId.ToString());

            using var tagReader = await tagCommand.ExecuteReaderAsync();
            while (await tagReader.ReadAsync())
            {
                task.Tags.Add(tagReader.GetString(0));
            }
        }

        return task;
    }

    public async Task<IEnumerable<TaskEntity>> QueryAsync(TaskQuerySpec spec)
    {
        if (spec == null) throw new ArgumentNullException(nameof(spec));

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var whereClauses = new List<string>();
        var parameters = new List<SqliteParameter>();

        if (spec.StartDate.HasValue)
        {
            whereClauses.Add("DeadlineDate >= @StartDate");
            parameters.Add(new SqliteParameter("@StartDate", spec.StartDate.Value.ToString("yyyy-MM-dd")));
        }

        if (spec.EndDate.HasValue)
        {
            whereClauses.Add("DeadlineDate <= @EndDate");
            parameters.Add(new SqliteParameter("@EndDate", spec.EndDate.Value.ToString("yyyy-MM-dd")));
        }

        if (spec.TaskTypeId.HasValue)
        {
            whereClauses.Add("TaskTypeId = @TaskTypeId");
            parameters.Add(new SqliteParameter("@TaskTypeId", spec.TaskTypeId.Value));
        }

        if (spec.MinUrgency.HasValue)
        {
            whereClauses.Add("UrgencyLevel >= @MinUrgency");
            parameters.Add(new SqliteParameter("@MinUrgency", spec.MinUrgency.Value));
        }

        if (spec.MaxUrgency.HasValue)
        {
            whereClauses.Add("UrgencyLevel <= @MaxUrgency");
            parameters.Add(new SqliteParameter("@MaxUrgency", spec.MaxUrgency.Value));
        }

        if (spec.MinImportance.HasValue)
        {
            whereClauses.Add("Importance >= @MinImportance");
            parameters.Add(new SqliteParameter("@MinImportance", spec.MinImportance.Value));
        }

        if (spec.MaxImportance.HasValue)
        {
            whereClauses.Add("Importance <= @MaxImportance");
            parameters.Add(new SqliteParameter("@MaxImportance", spec.MaxImportance.Value));
        }

        if (spec.MinComplexity.HasValue)
        {
            whereClauses.Add("Complexity >= @MinComplexity");
            parameters.Add(new SqliteParameter("@MinComplexity", spec.MinComplexity.Value));
        }

        if (spec.MaxComplexity.HasValue)
        {
            whereClauses.Add("Complexity <= @MaxComplexity");
            parameters.Add(new SqliteParameter("@MaxComplexity", spec.MaxComplexity.Value));
        }

        var whereClause = whereClauses.Any() ? "WHERE " + string.Join(" AND ", whereClauses) : "";

        var sql = $@"
            SELECT t.Id, t.Name, t.Description, t.TaskTypeId, t.DeadlineDate, t.DeadlineTime, 
                   t.Importance, t.Complexity, t.UrgencyLevel, t.IsCompleted, t.CreatedAt, t.UpdatedAt,
                   tt.Name as TaskTypeName
            FROM Tasks t
            LEFT JOIN TaskTypes tt ON t.TaskTypeId = tt.Id
            {whereClause}";

        var tasks = new List<TaskEntity>();

        using (var command = connection.CreateCommand())
        {
            command.CommandText = sql;
            foreach (var param in parameters)
            {
                command.Parameters.Add(param);
            }

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tasks.Add(MapTaskFromReader(reader));
            }
        }

        // Load tags for all tasks
        foreach (var task in tasks)
        {
            using var tagCommand = connection.CreateCommand();
            tagCommand.CommandText = @"
                SELECT TagName
                FROM TaskTags
                WHERE TaskId = @TaskId";
            tagCommand.Parameters.AddWithValue("@TaskId", task.Id.ToString());

            using var tagReader = await tagCommand.ExecuteReaderAsync();
            while (await tagReader.ReadAsync())
            {
                task.Tags.Add(tagReader.GetString(0));
            }
        }

        // Filter by tags if specified
        if (spec.Tags != null && spec.Tags.Any())
        {
            tasks = tasks.Where(t => spec.Tags.All(tag => t.Tags.Contains(tag))).ToList();
        }

        return tasks;
    }

    private TaskEntity MapTaskFromReader(SqliteDataReader reader)
    {
        return new TaskEntity
        {
            Id = Guid.Parse(reader.GetString(0)),
            Name = reader.GetString(1),
            Description = reader.GetString(2),
            TaskTypeId = reader.GetInt32(3),
            DeadlineDate = reader.IsDBNull(4) ? null : DateTime.Parse(reader.GetString(4)),
            DeadlineTime = reader.IsDBNull(5) ? null : TimeSpan.Parse(reader.GetString(5)),
            Importance = reader.IsDBNull(6) ? null : reader.GetDouble(6),
            Complexity = reader.IsDBNull(7) ? null : reader.GetDouble(7),
            UrgencyLevel = reader.GetInt32(8),
            IsCompleted = reader.GetInt32(9) == 1,
            CreatedAt = DateTime.Parse(reader.GetString(10)),
            UpdatedAt = DateTime.Parse(reader.GetString(11)),
            TaskTypeName = reader.IsDBNull(12) ? string.Empty : reader.GetString(12)
        };
    }
}
