using System;
using System.Threading.Tasks;
using DayPlannerApp.Models;
using Microsoft.Data.Sqlite;

namespace DayPlannerApp.Repositories;

/// <summary>
/// Repository for IPC authentication token persistence.
/// </summary>
public class IPCAuthTokenRepository : IIPCAuthTokenRepository
{
    private readonly string _connectionString;

    public IPCAuthTokenRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public async Task InsertTokenAsync(IPCAuthToken token)
    {
        if (token == null)
            throw new ArgumentNullException(nameof(token));
        if (string.IsNullOrWhiteSpace(token.Token))
            throw new ArgumentException("Token value cannot be empty", nameof(token));

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO IPCAuthTokens (Token, Description, CreatedAt, ExpiresAt, LastUsedAt)
            VALUES (@Token, @Description, @CreatedAt, @ExpiresAt, @LastUsedAt)";
        
        command.Parameters.AddWithValue("@Token", token.Token);
        command.Parameters.AddWithValue("@Description", (object?)token.Description ?? DBNull.Value);
        command.Parameters.AddWithValue("@CreatedAt", token.CreatedAt.ToString("o"));
        command.Parameters.AddWithValue("@ExpiresAt", token.ExpiresAt?.ToString("o") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@LastUsedAt", token.LastUsedAt?.ToString("o") ?? (object)DBNull.Value);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<IPCAuthToken?> GetTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token cannot be empty", nameof(token));

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Token, Description, CreatedAt, ExpiresAt, LastUsedAt
            FROM IPCAuthTokens
            WHERE Token = @Token";
        command.Parameters.AddWithValue("@Token", token);

        using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        return new IPCAuthToken
        {
            Token = reader.GetString(0),
            Description = reader.IsDBNull(1) ? null : reader.GetString(1),
            CreatedAt = DateTime.Parse(reader.GetString(2)),
            ExpiresAt = reader.IsDBNull(3) ? null : DateTime.Parse(reader.GetString(3)),
            LastUsedAt = reader.IsDBNull(4) ? null : DateTime.Parse(reader.GetString(4))
        };
    }

    public async Task UpdateLastUsedAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token cannot be empty", nameof(token));

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE IPCAuthTokens
            SET LastUsedAt = @LastUsedAt
            WHERE Token = @Token";
        command.Parameters.AddWithValue("@Token", token);
        command.Parameters.AddWithValue("@LastUsedAt", DateTime.UtcNow.ToString("o"));

        await command.ExecuteNonQueryAsync();
    }

    public async Task<bool> AnyTokensExistAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM IPCAuthTokens";
        
        var count = (long)(await command.ExecuteScalarAsync() ?? 0L);
        return count > 0;
    }
}
