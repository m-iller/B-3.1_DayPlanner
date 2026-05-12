using Microsoft.Data.Sqlite;

namespace DayPlannerApp.Tests;

public class TestDatabaseHelper : IDisposable
{
    public SqliteConnection Connection { get; }
    public string ConnectionString => Connection.ConnectionString;

    public TestDatabaseHelper()
    {
        var dbName = $"test_{Guid.NewGuid():N}";
        Connection = new SqliteConnection($"Data Source={dbName};Mode=Memory;Cache=Shared");
        Connection.Open();
        InitializeSchema();
    }

    private void InitializeSchema()
    {
        using var command = Connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE TaskTypes (
                Id INTEGER PRIMARY KEY,
                Name TEXT NOT NULL,
                ColorHex TEXT NOT NULL
            );

            INSERT INTO TaskTypes (Id, Name, ColorHex) VALUES 
                (1, 'Personal Projects', '#FF5733'),
                (2, 'Learning', '#33FF57'),
                (3, 'Work', '#3357FF'),
                (4, 'Must Do', '#FF33F5');

            CREATE TABLE Tasks (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL DEFAULT '',
                Description TEXT NOT NULL,
                TaskTypeId INTEGER NOT NULL,
                DeadlineDate TEXT,
                DeadlineTime TEXT,
                Importance REAL,
                Complexity REAL,
                UrgencyLevel INTEGER NOT NULL DEFAULT 0,
                IsCompleted INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                FOREIGN KEY (TaskTypeId) REFERENCES TaskTypes(Id)
            );

            CREATE TABLE Tags (
                Name TEXT PRIMARY KEY,
                CreatedAt TEXT NOT NULL
            );

            CREATE TABLE TaskTags (
                TaskId TEXT NOT NULL,
                TagName TEXT NOT NULL,
                PRIMARY KEY (TaskId, TagName),
                FOREIGN KEY (TaskId) REFERENCES Tasks(Id) ON DELETE CASCADE,
                FOREIGN KEY (TagName) REFERENCES Tags(Name) ON DELETE CASCADE
            );

            CREATE TABLE TimeTrackingSessions (
                Id TEXT PRIMARY KEY,
                TaskId TEXT NOT NULL,
                StartTime TEXT NOT NULL,
                EndTime TEXT,
                TotalDuration TEXT NOT NULL,
                TotalBreakTime TEXT NOT NULL,
                FOREIGN KEY (TaskId) REFERENCES Tasks(Id) ON DELETE CASCADE
            );

            CREATE TABLE BreakPeriods (
                Id TEXT PRIMARY KEY,
                SessionId TEXT NOT NULL,
                StartTime TEXT NOT NULL,
                EndTime TEXT,
                FOREIGN KEY (SessionId) REFERENCES TimeTrackingSessions(Id) ON DELETE CASCADE
            );

            CREATE TABLE Configuration (
                Key TEXT PRIMARY KEY,
                Value TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );

            CREATE TABLE Modules (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                Version TEXT NOT NULL,
                Description TEXT,
                AssemblyPath TEXT NOT NULL,
                IsEnabled INTEGER NOT NULL DEFAULT 1,
                LoadedAt TEXT,
                CreatedAt TEXT NOT NULL
            );

            CREATE TABLE IPCAuthTokens (
                Token TEXT PRIMARY KEY,
                Description TEXT,
                CreatedAt TEXT NOT NULL,
                ExpiresAt TEXT,
                LastUsedAt TEXT
            );

            CREATE INDEX idx_tasks_deadline ON Tasks(DeadlineDate);
            CREATE INDEX idx_tasks_type ON Tasks(TaskTypeId);
            CREATE INDEX idx_tasks_urgency ON Tasks(UrgencyLevel);
            CREATE INDEX idx_tasks_coordinates ON Tasks(Importance, Complexity);
            CREATE INDEX idx_time_sessions_task ON TimeTrackingSessions(TaskId);
        ";
        command.ExecuteNonQuery();
    }

    public void Dispose()
    {
        Connection?.Dispose();
    }
}
