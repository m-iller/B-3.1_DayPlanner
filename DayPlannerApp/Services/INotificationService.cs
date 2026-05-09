namespace DayPlannerApp.Services;

/// <summary>
/// Service for displaying user notifications
/// </summary>
public interface INotificationService
{
    void ShowInfo(string message, string title = "Information");
    void ShowWarning(string message, string title = "Warning");
    void ShowError(string message, string title = "Error");
    bool ShowConfirmation(string message, string title = "Confirm");
}
