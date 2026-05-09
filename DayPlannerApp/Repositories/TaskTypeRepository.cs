using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DayPlannerApp.Models;
using Microsoft.Data.Sqlite;

namespace DayPlannerApp.Repositories;

public class TaskTypeRepository : ITaskTypeRepository
{
    private readonly string _connectionString;

    public TaskTypeRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public async Task<TaskType?> GetTaskTypeAsync(int typeId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, Name, ColorHex
            FROM TaskTypes
            WHERE Id = @Id";
        command.Parameters.AddWithValue("@Id", typeId);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new TaskType
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                ColorHex = reader.GetString(2)
            };
        }

        return null;
    }

    public async Task<IEnumerable<TaskType>> GetAllTaskTypesAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var taskTypes = new List<TaskType>();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, ColorHex FROM TaskTypes ORDER BY Id";

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            taskTypes.Add(new TaskType
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                ColorHex = reader.GetString(2)
            });
        }

        return taskTypes;
    }

    public async Task UpdateTaskTypeNameAsync(int typeId, string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Task type name cannot be empty", nameof(newName));

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE TaskTypes
            SET Name = @Name
            WHERE Id = @Id";
        command.Parameters.AddWithValue("@Id", typeId);
        command.Parameters.AddWithValue("@Name", newName);

        await command.ExecuteNonQueryAsync();
    }
}
