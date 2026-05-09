using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DayPlannerApp.Models;
using Microsoft.Data.Sqlite;

namespace DayPlannerApp.Repositories;

public class TagRepository : ITagRepository
{
    private readonly string _connectionString;

    public TagRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public async Task<Tag> CreateTagAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tag name cannot be empty", nameof(name));

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var tag = new Tag
        {
            Name = name,
            CreatedAt = DateTime.UtcNow
        };

        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Tags (Name, CreatedAt)
            VALUES (@Name, @CreatedAt)";
        command.Parameters.AddWithValue("@Name", tag.Name);
        command.Parameters.AddWithValue("@CreatedAt", tag.CreatedAt.ToString("o"));

        await command.ExecuteNonQueryAsync();
        return tag;
    }

    public async Task DeleteTagAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tag name cannot be empty", nameof(name));

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Tags WHERE Name = @Name";
        command.Parameters.AddWithValue("@Name", name);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<IEnumerable<Tag>> GetAllTagsAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var tags = new List<Tag>();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Name, CreatedAt FROM Tags ORDER BY Name";

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tags.Add(new Tag
            {
                Name = reader.GetString(0),
                CreatedAt = DateTime.Parse(reader.GetString(1))
            });
        }

        return tags;
    }

    public async Task<IEnumerable<Tag>> GetTagsForTaskAsync(Guid taskId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var tags = new List<Tag>();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT t.Name, t.CreatedAt
            FROM Tags t
            INNER JOIN TaskTags tt ON t.Name = tt.TagName
            WHERE tt.TaskId = @TaskId
            ORDER BY t.Name";
        command.Parameters.AddWithValue("@TaskId", taskId.ToString());

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tags.Add(new Tag
            {
                Name = reader.GetString(0),
                CreatedAt = DateTime.Parse(reader.GetString(1))
            });
        }

        return tags;
    }

    public async Task AssignTagToTaskAsync(Guid taskId, string tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName))
            throw new ArgumentException("Tag name cannot be empty", nameof(tagName));

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();
        try
        {
            // Ensure tag exists
            using (var tagCommand = connection.CreateCommand())
            {
                tagCommand.Transaction = transaction;
                tagCommand.CommandText = @"
                    INSERT OR IGNORE INTO Tags (Name, CreatedAt)
                    VALUES (@Name, @CreatedAt)";
                tagCommand.Parameters.AddWithValue("@Name", tagName);
                tagCommand.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow.ToString("o"));
                await tagCommand.ExecuteNonQueryAsync();
            }

            // Link task to tag
            using (var linkCommand = connection.CreateCommand())
            {
                linkCommand.Transaction = transaction;
                linkCommand.CommandText = @"
                    INSERT OR IGNORE INTO TaskTags (TaskId, TagName)
                    VALUES (@TaskId, @TagName)";
                linkCommand.Parameters.AddWithValue("@TaskId", taskId.ToString());
                linkCommand.Parameters.AddWithValue("@TagName", tagName);
                await linkCommand.ExecuteNonQueryAsync();
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task RemoveTagFromTaskAsync(Guid taskId, string tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName))
            throw new ArgumentException("Tag name cannot be empty", nameof(tagName));

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            DELETE FROM TaskTags 
            WHERE TaskId = @TaskId AND TagName = @TagName";
        command.Parameters.AddWithValue("@TaskId", taskId.ToString());
        command.Parameters.AddWithValue("@TagName", tagName);
        await command.ExecuteNonQueryAsync();
    }
}
