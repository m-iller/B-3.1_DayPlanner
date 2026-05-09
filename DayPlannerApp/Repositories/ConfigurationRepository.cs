using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace DayPlannerApp.Repositories;

public class ConfigurationRepository : IConfigurationRepository
{
    private readonly string _connectionString;

    public ConfigurationRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public async Task<T?> GetSettingAsync<T>(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be empty", nameof(key));

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Value FROM Configuration WHERE Key = @Key";
        command.Parameters.AddWithValue("@Key", key);

        var result = await command.ExecuteScalarAsync();
        if (result == null || result == DBNull.Value)
            return default;

        var json = result.ToString();
        if (string.IsNullOrEmpty(json))
            return default;

        return JsonSerializer.Deserialize<T>(json);
    }

    public async Task SetSettingAsync<T>(string key, T value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be empty", nameof(key));

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var json = JsonSerializer.Serialize(value);

        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Configuration (Key, Value, UpdatedAt)
            VALUES (@Key, @Value, @UpdatedAt)
            ON CONFLICT(Key) DO UPDATE SET
                Value = @Value,
                UpdatedAt = @UpdatedAt";
        command.Parameters.AddWithValue("@Key", key);
        command.Parameters.AddWithValue("@Value", json);
        command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow.ToString("o"));

        await command.ExecuteNonQueryAsync();
    }
}
