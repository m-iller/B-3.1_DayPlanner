using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace DayPlannerApp.Views;

public partial class TaskEditorView : UserControl
{
    public TaskEditorView()
    {
        InitializeComponent();
    }
}

public class BoolToEditModeTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isEditMode)
        {
            return isEditMode ? "Edit Task" : "Create New Task";
        }
        return "Task Editor";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
