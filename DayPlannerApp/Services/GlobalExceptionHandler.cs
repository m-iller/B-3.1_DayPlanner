using System.Windows;
using System.Windows.Threading;

namespace DayPlannerApp.Services;

/// <summary>
/// Handles unhandled exceptions globally
/// </summary>
public class GlobalExceptionHandler
{
    private readonly ILogger _logger;
    private readonly INotificationService _notificationService;

    public GlobalExceptionHandler(ILogger logger, INotificationService notificationService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
    }

    public void Initialize(Application app)
    {
        app.DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _logger.Error("Unhandled dispatcher exception", e.Exception);
        
        _notificationService.ShowError(
            $"An unexpected error occurred:\n\n{e.Exception.Message}\n\nThe application will continue running, but some features may not work correctly.",
            "Unexpected Error");

        e.Handled = true;
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            _logger.Fatal("Unhandled application exception", ex);
            
            MessageBox.Show(
                $"A fatal error occurred:\n\n{ex.Message}\n\nThe application will now close.",
                "Fatal Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
