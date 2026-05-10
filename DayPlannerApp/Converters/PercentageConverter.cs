using System;
using System.Globalization;
using System.Windows.Data;

namespace DayPlannerApp.Converters;

public class PercentageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double actualValue && parameter is string percentStr)
        {
            if (double.TryParse(percentStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var percent))
            {
                return actualValue * percent;
            }
        }
        return 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
