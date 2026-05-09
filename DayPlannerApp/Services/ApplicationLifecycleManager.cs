using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DayPlannerApp.Data;
using DayPlannerApp.Repositories;

namespace DayPlannerApp.Services;

public class ApplicationLifecycleManager
{
    private readonly DatabaseInitializer _databaseInitializer;
    private readonly ITimeTracker _timeTracker;
    private readonly IConfigurationRepository _configurationRepository;
    private readonly ILogger _logger;

    public ApplicationLifecycleManager(
        DatabaseInitializer databaseInitializer,
        ITimeTracker timeTracker,
        IConfigurationRepository configurationRepository,
        ILogger logger)
    {
        _databaseInitializer = databaseInitializer ?? throw new ArgumentNullException(nameof(databaseInitializer));
        _timeTracker = timeTracker ?? throw new ArgumentNullException(nameof(timeTracker));
        _configurationRepository = configurationRepository ?? throw new ArgumentNullException(nameof(configurationRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<StartupResult> StartupAsync()
    {
        try
        {
            _logger.Info("Starting application lifecycle");

            // Initialize database
            _logger.Info("Initializing database");
            await _databaseInitializer.InitializeAsync();

            // Validate database integrity
            _logger.Info("Validating database integrity");
            var isValid = await _databaseInitializer.ValidateDatabaseIntegrityAsync();
            if (!isValid)
            {
                _logger.Error("Database integrity check failed");
                return new StartupResult
                {
                    Success = false,
                    ErrorMessage = "Database integrity check failed. The database may be corrupted.",
                    ErrorType = StartupErrorType.DatabaseCorruption
                };
            }

            // Load configuration settings
            _logger.Info("Loading configuration");
            await LoadConfigurationAsync();

            _logger.Info("Application lifecycle startup complete");
            return new StartupResult { Success = true };
        }
        catch (DatabaseInitializationException ex)
        {
            _logger.Error("Database initialization failed", ex);
            return new StartupResult
            {
                Success = false,
                ErrorMessage = $"Failed to initialize database: {ex.Message}",
                ErrorType = StartupErrorType.DatabaseInitializationFailed,
                Exception = ex
            };
        }
        catch (Exception ex)
        {
            _logger.Error("Unexpected error during startup", ex);
            return new StartupResult
            {
                Success = false,
                ErrorMessage = $"Unexpected error during startup: {ex.Message}",
                ErrorType = StartupErrorType.UnexpectedError,
                Exception = ex
            };
        }
    }

    public async Task ShutdownAsync()
    {
        try
        {
            _logger.Info("Starting application shutdown");

            // Stop all active time tracking sessions
            _logger.Info("Stopping active time tracking sessions");
            await StopActiveTimeTrackingSessionsAsync();

            // Save configuration
            _logger.Info("Saving configuration");
            await SaveConfigurationAsync();

            _logger.Info("Application shutdown complete");
        }
        catch (Exception ex)
        {
            _logger.Error("Error during shutdown", ex);
            // Don't throw - allow shutdown to complete
        }
    }

    private async Task LoadConfigurationAsync()
    {
        // Load any startup configuration settings
        // Currently a placeholder for future configuration needs
        await Task.CompletedTask;
    }

    private async Task SaveConfigurationAsync()
    {
        // Save any configuration that needs to persist
        var lastShutdown = DateTime.UtcNow;
        await _configurationRepository.SetSettingAsync("LastShutdown", lastShutdown);
    }

    private async Task StopActiveTimeTrackingSessionsAsync()
    {
        // Get all active sessions and stop them gracefully
        if (_timeTracker is TimeTracker tracker)
        {
            await tracker.StopAllActiveSessionsAsync();
        }
    }
}

public class StartupResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public StartupErrorType ErrorType { get; set; }
    public Exception? Exception { get; set; }
}

public enum StartupErrorType
{
    None,
    DatabaseInitializationFailed,
    DatabaseCorruption,
    ConfigurationLoadFailed,
    UnexpectedError
}
