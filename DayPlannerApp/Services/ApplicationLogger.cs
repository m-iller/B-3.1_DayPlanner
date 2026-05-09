using Serilog;
using Serilog.Core;

namespace DayPlannerApp.Services;

/// <summary>
/// Application logger implementation using Serilog
/// </summary>
public class ApplicationLogger : ILogger
{
    private readonly Logger _logger;

    public ApplicationLogger(string logFilePath)
    {
        _logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                logFilePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.Debug()
            .CreateLogger();
    }

    public void Debug(string message)
    {
        _logger.Debug(message);
    }

    public void Info(string message)
    {
        _logger.Information(message);
    }

    public void Warning(string message)
    {
        _logger.Warning(message);
    }

    public void Error(string message, Exception? exception = null)
    {
        if (exception != null)
        {
            _logger.Error(exception, message);
        }
        else
        {
            _logger.Error(message);
        }
    }

    public void Fatal(string message, Exception? exception = null)
    {
        if (exception != null)
        {
            _logger.Fatal(exception, message);
        }
        else
        {
            _logger.Fatal(message);
        }
    }

    public void Dispose()
    {
        _logger.Dispose();
    }
}
