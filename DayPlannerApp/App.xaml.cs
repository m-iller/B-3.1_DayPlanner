using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using DayPlannerApp.Data;
using DayPlannerApp.Services;
using DayPlannerApp.Repositories;
using DayPlannerApp.ViewModels;

namespace DayPlannerApp;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private ServiceProvider? _serviceProvider;
    private ApplicationLifecycleManager? _lifecycleManager;
    private ILogger? _logger;
    private GlobalExceptionHandler? _exceptionHandler;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            // Configure dependency injection
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            // Initialize logger
            _logger = _serviceProvider.GetRequiredService<ILogger>();
            _logger.Info("Application starting");

            // Initialize global exception handler
            _exceptionHandler = _serviceProvider.GetRequiredService<GlobalExceptionHandler>();
            _exceptionHandler.Initialize(this);

            // Get lifecycle manager
            _lifecycleManager = _serviceProvider.GetRequiredService<ApplicationLifecycleManager>();

            // Execute startup sequence
            var startupResult = await _lifecycleManager.StartupAsync();

            if (!startupResult.Success)
            {
                _logger.Error($"Startup failed: {startupResult.ErrorMessage}", startupResult.Exception);
                HandleStartupFailure(startupResult);
                Shutdown(1);
                return;
            }

            _logger.Info("Application started successfully");

            // Show main window
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            _logger?.Fatal("Fatal error during application startup", ex);
            MessageBox.Show(
                $"Fatal error during application startup:\n\n{ex.Message}",
                "Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Configure logging
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DayPlannerApp");
        Directory.CreateDirectory(appDataPath);
        var logFilePath = Path.Combine(appDataPath, "logs", "app-.log");
        Directory.CreateDirectory(Path.GetDirectoryName(logFilePath)!);
        
        services.AddSingleton<ILogger>(new ApplicationLogger(logFilePath));

        // Register notification service
        services.AddSingleton<INotificationService, NotificationService>();

        // Register global exception handler
        services.AddSingleton<GlobalExceptionHandler>();

        // Register database initializer
        services.AddSingleton<DatabaseInitializer>();

        // Register data change notifier
        services.AddSingleton<DataChangeNotifier>();

        // Register repositories
        services.AddSingleton<ITaskRepository>(sp =>
        {
            var dbInit = sp.GetRequiredService<DatabaseInitializer>();
            return new TaskRepository(dbInit.ConnectionString);
        });

        services.AddSingleton<ITimeTrackingRepository>(sp =>
        {
            var dbInit = sp.GetRequiredService<DatabaseInitializer>();
            return new TimeTrackingRepository(dbInit.ConnectionString);
        });

        services.AddSingleton<ITagRepository>(sp =>
        {
            var dbInit = sp.GetRequiredService<DatabaseInitializer>();
            return new TagRepository(dbInit.ConnectionString);
        });

        services.AddSingleton<IConfigurationRepository>(sp =>
        {
            var dbInit = sp.GetRequiredService<DatabaseInitializer>();
            return new ConfigurationRepository(dbInit.ConnectionString);
        });

        services.AddSingleton<ITaskTypeRepository>(sp =>
        {
            var dbInit = sp.GetRequiredService<DatabaseInitializer>();
            return new TaskTypeRepository(dbInit.ConnectionString);
        });

        // Register services
        services.AddSingleton<ITimeTracker, TimeTracker>();
        services.AddSingleton<ITaskManager, TaskManager>();
        services.AddSingleton<ITagManager, TagManager>();
        services.AddSingleton<ITaskTypeManager, TaskTypeManager>();
        services.AddSingleton<IMarkdownProcessor, MarkdownProcessor>();
        services.AddSingleton<ICoordinateController, CoordinateController>();
        services.AddSingleton<ICalendarViewManager, CalendarViewManager>();

        // Register lifecycle manager
        services.AddSingleton<ApplicationLifecycleManager>();

        // Register ViewModels
        services.AddSingleton<MainViewModel>();

        // Register main window
        services.AddSingleton<MainWindow>();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        try
        {
            _logger?.Info("Application shutting down");
            
            if (_lifecycleManager != null)
            {
                await _lifecycleManager.ShutdownAsync();
            }

            _logger?.Info("Application shutdown complete");
        }
        catch (Exception ex)
        {
            _logger?.Error("Error during shutdown", ex);
        }
        finally
        {
            _serviceProvider?.Dispose();
            base.OnExit(e);
        }
    }

    private void HandleStartupFailure(StartupResult result)
    {
        var message = result.ErrorType switch
        {
            StartupErrorType.DatabaseCorruption => 
                $"{result.ErrorMessage}\n\nWould you like to start with an empty database?\n\n" +
                "Warning: This will create a new database file. Your existing data will not be deleted " +
                "but will need to be manually recovered.",
            
            StartupErrorType.DatabaseInitializationFailed =>
                $"{result.ErrorMessage}\n\nThe application cannot start without a valid database.\n\n" +
                "Please check that you have write permissions to your application data folder.",
            
            _ => 
                $"{result.ErrorMessage}\n\nThe application cannot start."
        };

        if (result.ErrorType == StartupErrorType.DatabaseCorruption)
        {
            var dialogResult = MessageBox.Show(
                message,
                "Database Error",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (dialogResult == MessageBoxResult.Yes)
            {
                // User chose to start with empty database
                TryCreateEmptyDatabase();
            }
        }
        else
        {
            MessageBox.Show(
                message,
                "Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void TryCreateEmptyDatabase()
    {
        try
        {
            var dbInit = _serviceProvider?.GetRequiredService<DatabaseInitializer>();
            if (dbInit != null)
            {
                // Rename corrupted database
                var backupPath = dbInit.DatabasePath + ".corrupted." + DateTime.Now.ToString("yyyyMMddHHmmss");
                if (System.IO.File.Exists(dbInit.DatabasePath))
                {
                    System.IO.File.Move(dbInit.DatabasePath, backupPath);
                }

                // Restart application
                System.Diagnostics.Process.Start(
                    System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "DayPlannerApp.exe");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to create empty database: {ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}

