using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DayPlannerApp.Data;

public class DatabaseInitializer
{
    private const string DATABASE_FILENAME = "dayplanner.db";
    private readonly string _connectionString;

    public DatabaseInitializer()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dbDirectory = Path.Combine(appDataPath, "DayPlannerApp");
        Directory.CreateDirectory(dbDirectory);
        
        var dbPath = Path.Combine(dbDirectory, DATABASE_FILENAME);
        _connectionString = $"Data Source={dbPath}";
    }

    public string ConnectionString => _connectionString;

    public async Task InitializeAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        // Enable foreign keys
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "PRAGMA foreign_keys = ON;";
            await command.ExecuteNonQueryAsync();
        }

        // Create tables
        await CreateTablesAsync(connection);
        
        // Seed default data
        await SeedDefaultDataAsync(connection);
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
}
