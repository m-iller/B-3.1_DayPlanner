using System;
using System.Globalization;
using System.Windows.Data;

namespace DayPlannerApp.Converters;

public class PercentageToWidthConverter : IValueConverter
{
    private const double MAX_WIDTH = 300.0;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double percentage)
        {
            return (percentage / 100.0) * MAX_WIDTH;
        }
        return 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
