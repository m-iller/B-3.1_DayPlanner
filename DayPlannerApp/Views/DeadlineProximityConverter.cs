using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DayPlannerApp.Views;

/// <summary>
/// Converts deadline date to color based on proximity to current date
/// </summary>
public class DeadlineProximityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not DateTime deadlineDate)
        {
            return new SolidColorBrush(Colors.Gray);
        }

        var daysUntilDeadline = (deadlineDate.Date - DateTime.Today).TotalDays;

        if (daysUntilDeadline < 0)
        {
            // Overdue - Red
            return new SolidColorBrush(Color.FromRgb(244, 67, 54));
        }
        else if (daysUntilDeadline <= 1)
        {
            // Due today or tomorrow - Dark Orange
            return new SolidColorBrush(Color.FromRgb(255, 87, 34));
        }
        else if (daysUntilDeadline <= 3)
        {
            // Due within 3 days - Orange
            return new SolidColorBrush(Color.FromRgb(255, 152, 0));
        }
        else if (daysUntilDeadline <= 7)
        {
            // Due within a week - Amber
            return new SolidColorBrush(Color.FromRgb(255, 193, 7));
        }
        else
        {
            // More than a week - Green
            return new SolidColorBrush(Color.FromRgb(76, 175, 80));
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts deadline date to text indicator based on proximity
/// </summary>
public class DeadlineProximityTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not DateTime deadlineDate)
        {
            return string.Empty;
        }

        var daysUntilDeadline = (deadlineDate.Date - DateTime.Today).TotalDays;

        if (daysUntilDeadline < 0)
        {
            var daysOverdue = Math.Abs(daysUntilDeadline);
            return daysOverdue == 1 ? "⚠ OVERDUE (1 day)" : $"⚠ OVERDUE ({daysOverdue:F0} days)";
        }
        else if (daysUntilDeadline == 0)
        {
            return "🔥 DUE TODAY";
        }
        else if (daysUntilDeadline == 1)
        {
            return "⚡ DUE TOMORROW";
        }
        else if (daysUntilDeadline <= 3)
        {
            return $"⏰ {daysUntilDeadline:F0} days left";
        }
        else if (daysUntilDeadline <= 7)
        {
            return $"📅 {daysUntilDeadline:F0} days left";
        }
        else
        {
            return $"✓ {daysUntilDeadline:F0} days left";
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
