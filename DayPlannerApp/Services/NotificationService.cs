using System.Windows;

namespace DayPlannerApp.Services;

/// <summary>
/// WPF MessageBox-based notification service
/// </summary>
public class NotificationService : INotificationService
{
    private readonly ILogger _logger;

    public NotificationService(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void ShowInfo(string message, string title = "Information")
    {
        _logger.Info($"Notification: {title} - {message}");
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public void ShowWarning(string message, string title = "Warning")
    {
        _logger.Warning($"Notification: {title} - {message}");
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    public void ShowError(string message, string title = "Error")
    {
        _logger.Error($"Notification: {title} - {message}");
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public bool ShowConfirmation(string message, string title = "Confirm")
    {
        _logger.Info($"Confirmation dialog: {title} - {message}");
        var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
        return result == MessageBoxResult.Yes;
    }
}
