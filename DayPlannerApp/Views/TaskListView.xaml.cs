using System.Windows.Controls;
using DayPlannerApp.ViewModels;

namespace DayPlannerApp.Views;

public partial class TaskListView : UserControl
{
    public TaskListView()
    {
        InitializeComponent();
    }

    private async void TagButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button && button.Tag is string tagName)
        {
            var viewModel = DataContext as TaskListViewModel;
            if (viewModel != null)
            {
                viewModel.FilterTag = tagName;
                await viewModel.LoadTasksAsync();
                // Filter by the clicked tag
                viewModel.FilterByTagCommand.Execute(tagName);
            }
        }
    }
}
