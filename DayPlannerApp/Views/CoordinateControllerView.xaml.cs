using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using DayPlannerApp.ViewModels;

namespace DayPlannerApp.Views;

public partial class CoordinateControllerView : UserControl
{
    public CoordinateControllerView()
    {
        InitializeComponent();
    }

    private void TaskBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is System.Windows.FrameworkElement element && element.Tag is TaskCoordinateViewModel task)
        {
            var viewModel = DataContext as CoordinateControllerViewModel;
            viewModel?.OpenTaskCommand.Execute(task);
        }
    }
}

public class PercentageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double actualValue && parameter is string percentStr)
        {
            if (double.TryParse(percentStr, out double percent))
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

public class CoordinateToCanvasConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length == 2 && values[0] is double coordinate && values[1] is double canvasSize)
        {
            // Convert 0-100 coordinate to canvas position
            return (coordinate / 100.0) * canvasSize;
        }
        return 0.0;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class InvertedCoordinateToCanvasConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length == 2 && values[0] is double coordinate && values[1] is double canvasSize)
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
