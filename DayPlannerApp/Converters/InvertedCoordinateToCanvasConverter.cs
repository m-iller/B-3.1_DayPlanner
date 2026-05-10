using System;
using System.Globalization;
using System.Windows.Data;

namespace DayPlannerApp.Converters;

public class InvertedCoordinateToCanvasConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length == 2 && 
            values[0] is double coordinate && 
            values[1] is double canvasSize)
        {
            // Convert 0-100 coordinate to canvas position (inverted for Y-axis)
            return canvasSize - ((coordinate / 100.0) * canvasSize);
        }
        return 0.0;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
