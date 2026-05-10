using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DayPlannerApp.Models;
using Microsoft.Data.Sqlite;

namespace DayPlannerApp.Repositories;

public class ModuleRepository : IModuleRepository
{
    private readonly string _connectionString;

    public ModuleRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public async Task<ModuleInfo> InsertAsync(ModuleInfo module)
    {
        if (module == null) throw new ArgumentNullException(nameof(module));
        if (string.IsNullOrWhiteSpace(module.Id)) throw new ArgumentException("Module ID cannot be empty", nameof(module));

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Modules (Id, Name, Version, Description, AssemblyPath, IsEnabled, LoadedAt, CreatedAt)
            VALUES (@Id, @Name, @Version, @Description, @AssemblyPath, @IsEnabled, @LoadedAt, @CreatedAt)";

        command.Parameters.AddWithValue("@Id", module.Id);
        command.Parameters.AddWithValue("@Name", module.Name);
        command.Parameters.AddWithValue("@Version", module.Version);
        command.Parameters.AddWithValue("@Description", module.Description ?? string.Empty);
        command.Parameters.AddWithValue("@AssemblyPath", module.AssemblyPath);
        command.Parameters.AddWithValue("@IsEnabled", module.IsEnabled ? 1 : 0);
        command.Parameters.AddWithValue("@LoadedAt", module.IsLoaded ? module.LoadedAt.ToString("o") : (object)DBNull.Value);
        command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow.ToString("o"));

        await command.ExecuteNonQueryAsync();
        return module;
    }

    public async Task<ModuleInfo> UpdateAsync(ModuleInfo module)
    {
        if (module == null) throw new ArgumentNullException(nameof(module));
        if (string.IsNullOrWhiteSpace(module.Id)) throw new ArgumentException("Module ID cannot be empty", nameof(module));

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE Modules 
            SET Name = @Name,
                Version = @Version,
                Description = @Description,
                AssemblyPath = @AssemblyPath,
                IsEnabled = @IsEnabled,
                LoadedAt = @LoadedAt
            WHERE Id = @Id";

        command.Parameters.AddWithValue("@Id", module.Id);
        command.Parameters.AddWithValue("@Name", module.Name);
        command.Parameters.AddWithValue("@Version", module.Version);
        command.Parameters.AddWithValue("@Description", module.Description ?? string.Empty);
        command.Parameters.AddWithValue("@AssemblyPath", module.AssemblyPath);
        command.Parameters.AddWithValue("@IsEnabled", module.IsEnabled ? 1 : 0);
        command.Parameters.AddWithValue("@LoadedAt", module.IsLoaded ? module.LoadedAt.ToString("o") : (object)DBNull.Value);

        await command.ExecuteNonQueryAsync();
        return module;
    }

    public async Task DeleteAsync(string moduleId)
    {
        if (string.IsNullOrWhiteSpace(moduleId)) throw new ArgumentException("Module ID cannot be empty", nameof(moduleId));

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Modules WHERE Id = @Id";
        command.Parameters.AddWithValue("@Id", moduleId);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<ModuleInfo?> GetByIdAsync(string moduleId)
    {
        if (string.IsNullOrWhiteSpace(moduleId)) return null;

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, Name, Version, Description, AssemblyPath, IsEnabled, LoadedAt, CreatedAt
            FROM Modules
            WHERE Id = @Id";
        command.Parameters.AddWithValue("@Id", moduleId);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapModuleFromReader(reader);
        }

        return null;
    }

    public async Task<IEnumerable<ModuleInfo>> GetAllAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, Name, Version, Description, AssemblyPath, IsEnabled, LoadedAt, CreatedAt
            FROM Modules
            ORDER BY Name";

        var modules = new List<ModuleInfo>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            modules.Add(MapModuleFromReader(reader));
        }

        return modules;
    }

    public async Task<IEnumerable<ModuleInfo>> GetEnabledAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, Name, Version, Description, AssemblyPath, IsEnabled, LoadedAt, CreatedAt
            FROM Modules
            WHERE IsEnabled = 1
            ORDER BY Name";

        var modules = new List<ModuleInfo>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            modules.Add(MapModuleFromReader(reader));
        }

        return modules;
    }

    private ModuleInfo MapModuleFromReader(SqliteDataReader reader)
    {
        var loadedAtStr = reader.IsDBNull(6) ? null : reader.GetString(6);
        
        return new ModuleInfo
        {
            Id = reader.GetString(0),
            Name = reader.GetString(1),
            Version = reader.GetString(2),
            Description = reader.GetString(3),
            AssemblyPath = reader.GetString(4),
            IsEnabled = reader.GetInt32(5) == 1,
            IsLoaded = !string.IsNullOrEmpty(loadedAtStr),
            LoadedAt = string.IsNullOrEmpty(loadedAtStr) 
                ? DateTime.MinValue 
                : DateTime.Parse(loadedAtStr, null, System.Globalization.DateTimeStyles.RoundtripKind)
        };
    }
}
