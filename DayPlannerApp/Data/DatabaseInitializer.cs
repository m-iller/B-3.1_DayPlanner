using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DayPlannerApp.Data;

public class DatabaseInitializer
{
    private const string DATABASE_FILENAME = "dayplanner.db";
    private const int CURRENT_SCHEMA_VERSION = 1;
    private readonly string _connectionString;
    private readonly string _databasePath;

    public DatabaseInitializer()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dbDirectory = Path.Combine(appDataPath, "DayPlannerApp");
        Directory.CreateDirectory(dbDirectory);
        
        _databasePath = Path.Combine(dbDirectory, DATABASE_FILENAME);
        _connectionString = $"Data Source={_databasePath}";
    }

    public string ConnectionString => _connectionString;
    public string DatabasePath => _databasePath;

    public async Task InitializeAsync()
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            // Enable foreign keys and WAL mode for better concurrency
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "PRAGMA foreign_keys = ON; PRAGMA journal_mode = WAL;";
                await command.ExecuteNonQueryAsync();
            }

            // Check schema version and migrate if needed
            var currentVersion = await GetSchemaVersionAsync(connection);
            if (currentVersion == 0)
            {
                // First run - create all tables
                await CreateTablesAsync(connection);
                await SetSchemaVersionAsync(connection, CURRENT_SCHEMA_VERSION);
                await SeedDefaultDataAsync(connection);
            }
            else if (currentVersion < CURRENT_SCHEMA_VERSION)
            {
                // Migration needed
                await MigrateSchemaAsync(connection, currentVersion, CURRENT_SCHEMA_VERSION);
            }
        }
        catch (SqliteException ex)
        {
            throw new DatabaseInitializationException("Failed to initialize database", ex);
        }
    }

    public async Task<bool> ValidateDatabaseIntegrityAsync()
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA integrity_check;";
            var result = await command.ExecuteScalarAsync();
            
            return result?.ToString() == "ok";
        }
        catch
        {
            return false;
        }
    }

    private async Task CreateTablesAsync(SqliteConnection connection)
    {
        var schema = @"
            CREATE TABLE IF NOT EXISTS TaskTypes (
                Id INTEGER PRIMARY KEY,
                Name TEXT NOT NULL,
                ColorHex TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Tasks (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL DEFAULT '',
                Description TEXT NOT NULL,
                TaskTypeId INTEGER NOT NULL,
                DeadlineDate TEXT,
                DeadlineTime TEXT,
                Importance REAL,
                Complexity REAL,
                UrgencyLevel INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                FOREIGN KEY (TaskTypeId) REFERENCES TaskTypes(Id)
            );

            CREATE TABLE IF NOT EXISTS Tags (
                Name TEXT PRIMARY KEY,
                CreatedAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS TaskTags (
                TaskId TEXT NOT NULL,
                TagName TEXT NOT NULL,
                PRIMARY KEY (TaskId, TagName),
                FOREIGN KEY (TaskId) REFERENCES Tasks(Id) ON DELETE CASCADE,
                FOREIGN KEY (TagName) REFERENCES Tags(Name) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS TimeTrackingSessions (
                Id TEXT PRIMARY KEY,
                TaskId TEXT NOT NULL,
                StartTime TEXT NOT NULL,
                EndTime TEXT,
                TotalDuration TEXT NOT NULL,
                TotalBreakTime TEXT NOT NULL,
                FOREIGN KEY (TaskId) REFERENCES Tasks(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS BreakPeriods (
                Id TEXT PRIMARY KEY,
                SessionId TEXT NOT NULL,
                StartTime TEXT NOT NULL,
                EndTime TEXT,
                FOREIGN KEY (SessionId) REFERENCES TimeTrackingSessions(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS Configuration (
                Key TEXT PRIMARY KEY,
                Value TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Modules (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                Version TEXT NOT NULL,
                Description TEXT,
                AssemblyPath TEXT NOT NULL,
                IsEnabled INTEGER NOT NULL DEFAULT 1,
                LoadedAt TEXT,
                CreatedAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS IPCAuthTokens (
                Token TEXT PRIMARY KEY,
                Description TEXT,
                CreatedAt TEXT NOT NULL,
                ExpiresAt TEXT,
                LastUsedAt TEXT
            );

            CREATE INDEX IF NOT EXISTS idx_tasks_deadline ON Tasks(DeadlineDate);
            CREATE INDEX IF NOT EXISTS idx_tasks_type ON Tasks(TaskTypeId);
            CREATE INDEX IF NOT EXISTS idx_tasks_urgency ON Tasks(UrgencyLevel);
            CREATE INDEX IF NOT EXISTS idx_tasks_coordinates ON Tasks(Importance, Complexity);
            CREATE INDEX IF NOT EXISTS idx_time_sessions_task ON TimeTrackingSessions(TaskId);
        ";

        using var command = connection.CreateCommand();
        command.CommandText = schema;
        await command.ExecuteNonQueryAsync();
    }

    private async Task SeedDefaultDataAsync(SqliteConnection connection)
    {
        // Check if task types already exist
        using (var checkCommand = connection.CreateCommand())
        {
            checkCommand.CommandText = "SELECT COUNT(*) FROM TaskTypes;";
            var count = (long)(await checkCommand.ExecuteScalarAsync() ?? 0L);
            
            if (count > 0)
            {
                return; // Already seeded
            }
        }

        // Seed default task types
        var seedData = @"
            INSERT INTO TaskTypes (Id, Name, ColorHex) VALUES
                (1, 'Personal Projects', '#3498db'),
                (2, 'Learning', '#2ecc71'),
                (3, 'Work', '#e74c3c'),
                (4, 'Must Do', '#f39c12');
        ";

        using var command = connection.CreateCommand();
        command.CommandText = seedData;
        await command.ExecuteNonQueryAsync();
    }

    private async Task<int> GetSchemaVersionAsync(SqliteConnection connection)
    {
        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT Value FROM Configuration WHERE Key = 'SchemaVersion';";
            var result = await command.ExecuteScalarAsync();
            
            if (result != null && int.TryParse(result.ToString()?.Trim('"'), out var version))
            {
                return version;
            }
        }
        catch (SqliteException)
        {
            // Configuration table doesn't exist yet
        }
        
        return 0;
    }

    private async Task SetSchemaVersionAsync(SqliteConnection connection, int version)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Configuration (Key, Value, UpdatedAt)
            VALUES ('SchemaVersion', @Version, @UpdatedAt)
            ON CONFLICT(Key) DO UPDATE SET
                Value = @Version,
                UpdatedAt = @UpdatedAt";
        command.Parameters.AddWithValue("@Version", version.ToString());
        command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow.ToString("o"));
        await command.ExecuteNonQueryAsync();
    }

    private async Task MigrateSchemaAsync(SqliteConnection connection, int fromVersion, int toVersion)
    {
        // Future migrations would go here
        // For now, just update version
        await SetSchemaVersionAsync(connection, toVersion);
    }
}

public class DatabaseInitializationException : Exception
{
    public DatabaseInitializationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
