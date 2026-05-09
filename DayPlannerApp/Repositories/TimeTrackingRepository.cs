using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DayPlannerApp.Models;
using Microsoft.Data.Sqlite;

namespace DayPlannerApp.Repositories;

public class TimeTrackingRepository : ITimeTrackingRepository
{
    private readonly string _connectionString;

    public TimeTrackingRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public async Task<TimeTrackingSession> InsertSessionAsync(TimeTrackingSession session)
    {
        if (session == null) throw new ArgumentNullException(nameof(session));

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();
        try
        {
            // Insert session
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
                    INSERT INTO TimeTrackingSessions (Id, TaskId, StartTime, EndTime, TotalDuration, TotalBreakTime)
                    VALUES (@Id, @TaskId, @StartTime, @EndTime, @TotalDuration, @TotalBreakTime)";

                command.Parameters.AddWithValue("@Id", session.Id.ToString());
                command.Parameters.AddWithValue("@TaskId", session.TaskId.ToString());
                command.Parameters.AddWithValue("@StartTime", session.StartTime.ToString("o"));
                command.Parameters.AddWithValue("@EndTime", session.EndTime?.ToString("o") ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@TotalDuration", session.TotalDuration.ToString());
                command.Parameters.AddWithValue("@TotalBreakTime", session.TotalBreakTime.ToString());

                await command.ExecuteNonQueryAsync();
            }

            // Insert break periods
            if (session.Breaks != null && session.Breaks.Any())
            {
                foreach (var breakPeriod in session.Breaks)
                {
                    using var breakCommand = connection.CreateCommand();
                    breakCommand.Transaction = transaction;
                    breakCommand.CommandText = @"
                        INSERT INTO BreakPeriods (Id, SessionId, StartTime, EndTime)
                        VALUES (@Id, @SessionId, @StartTime, @EndTime)";

                    breakCommand.Parameters.AddWithValue("@Id", breakPeriod.Id.ToString());
                    breakCommand.Parameters.AddWithValue("@SessionId", session.Id.ToString());
                    breakCommand.Parameters.AddWithValue("@StartTime", breakPeriod.StartTime.ToString("o"));
                    breakCommand.Parameters.AddWithValue("@EndTime", breakPeriod.EndTime?.ToString("o") ?? (object)DBNull.Value);

                    await breakCommand.ExecuteNonQueryAsync();
                }
            }

            transaction.Commit();
            return session;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<TimeTrackingSession> UpdateSessionAsync(TimeTrackingSession session)
    {
        if (session == null) throw new ArgumentNullException(nameof(session));

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();
        try
        {
            // Update session
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
                    UPDATE TimeTrackingSessions
                    SET EndTime = @EndTime,
                        TotalDuration = @TotalDuration,
                        TotalBreakTime = @TotalBreakTime
                    WHERE Id = @Id";

                command.Parameters.AddWithValue("@Id", session.Id.ToString());
                command.Parameters.AddWithValue("@EndTime", session.EndTime?.ToString("o") ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@TotalDuration", session.TotalDuration.ToString());
                command.Parameters.AddWithValue("@TotalBreakTime", session.TotalBreakTime.ToString());

                await command.ExecuteNonQueryAsync();
            }

            // Delete existing break periods
            using (var deleteCommand = connection.CreateCommand())
            {
                deleteCommand.Transaction = transaction;
                deleteCommand.CommandText = "DELETE FROM BreakPeriods WHERE SessionId = @SessionId";
                deleteCommand.Parameters.AddWithValue("@SessionId", session.Id.ToString());
                await deleteCommand.ExecuteNonQueryAsync();
            }

            // Insert updated break periods
            if (session.Breaks != null && session.Breaks.Any())
            {
                foreach (var breakPeriod in session.Breaks)
                {
                    using var breakCommand = connection.CreateCommand();
                    breakCommand.Transaction = transaction;
                    breakCommand.CommandText = @"
                        INSERT INTO BreakPeriods (Id, SessionId, StartTime, EndTime)
                        VALUES (@Id, @SessionId, @StartTime, @EndTime)";

                    breakCommand.Parameters.AddWithValue("@Id", breakPeriod.Id.ToString());
                    breakCommand.Parameters.AddWithValue("@SessionId", session.Id.ToString());
                    breakCommand.Parameters.AddWithValue("@StartTime", breakPeriod.StartTime.ToString("o"));
                    breakCommand.Parameters.AddWithValue("@EndTime", breakPeriod.EndTime?.ToString("o") ?? (object)DBNull.Value);

                    await breakCommand.ExecuteNonQueryAsync();
                }
            }

            transaction.Commit();
            return session;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<IEnumerable<TimeTrackingSession>> GetSessionsByTaskIdAsync(Guid taskId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var sessions = new List<TimeTrackingSession>();

        // Get sessions
        using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
                SELECT Id, TaskId, StartTime, EndTime, TotalDuration, TotalBreakTime
                FROM TimeTrackingSessions
                WHERE TaskId = @TaskId
                ORDER BY StartTime DESC";
            command.Parameters.AddWithValue("@TaskId", taskId.ToString());

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                sessions.Add(new TimeTrackingSession
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    TaskId = Guid.Parse(reader.GetString(1)),
                    StartTime = DateTime.Parse(reader.GetString(2)),
                    EndTime = reader.IsDBNull(3) ? null : DateTime.Parse(reader.GetString(3)),
                    TotalDuration = TimeSpan.Parse(reader.GetString(4)),
                    TotalBreakTime = TimeSpan.Parse(reader.GetString(5))
                });
            }
        }

        // Load break periods for each session
        foreach (var session in sessions)
        {
            using var breakCommand = connection.CreateCommand();
            breakCommand.CommandText = @"
                SELECT Id, StartTime, EndTime
                FROM BreakPeriods
                WHERE SessionId = @SessionId
                ORDER BY StartTime";
            breakCommand.Parameters.AddWithValue("@SessionId", session.Id.ToString());

            using var breakReader = await breakCommand.ExecuteReaderAsync();
            while (await breakReader.ReadAsync())
            {
                session.Breaks.Add(new BreakPeriod
                {
                    Id = Guid.Parse(breakReader.GetString(0)),
                    StartTime = DateTime.Parse(breakReader.GetString(1)),
                    EndTime = breakReader.IsDBNull(2) ? null : DateTime.Parse(breakReader.GetString(2))
                });
            }
        }

        return sessions;
    }
}
