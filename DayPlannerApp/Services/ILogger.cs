namespace DayPlannerApp.Services;

/// <summary>
/// Application logging interface
/// </summary>
public interface ILogger
{
    void Debug(string message);
    void Info(string message);
    void Warning(string message);
    void Error(string message, Exception? exception = null);
    void Fatal(string message, Exception? exception = null);
}
