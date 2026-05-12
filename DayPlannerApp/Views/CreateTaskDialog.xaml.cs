using System;
using System.Linq;
using System.Windows;
using DayPlannerApp.Models;
using DayPlannerApp.Services;

namespace DayPlannerApp.Views;

public partial class CreateTaskDialog : Window
{
    private readonly ITaskManager _taskManager;
    private readonly ITaskTypeManager _taskTypeManager;
    private readonly ITagManager _tagManager;
    
    public TaskEntity? CreatedTask { get; private set; }

    public CreateTaskDialog(
        ITaskManager taskManager,
        ITaskTypeManager taskTypeManager,
        ITagManager tagManager)
    {
        InitializeComponent();
        
        _taskManager = taskManager ?? throw new ArgumentNullException(nameof(taskManager));
        _taskTypeManager = taskTypeManager ?? throw new ArgumentNullException(nameof(taskTypeManager));
        _tagManager = tagManager ?? throw new ArgumentNullException(nameof(tagManager));
        
        Loaded += CreateTaskDialog_Loaded;
    }

    private async void CreateTaskDialog_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // Load task types
            var taskTypes = await _taskTypeManager.GetAllTaskTypesAsync();
            TaskTypeComboBox.ItemsSource = taskTypes;
            TaskTypeComboBox.SelectedIndex = 0;

            // Load all available tags for the dropdown
            var allTags = await _tagManager.GetAllTagsAsync();
            var tagNames = allTags.Select(t => t.Name).ToList();
            AvailableTagsComboBox.ItemsSource = tagNames;

            // Initialize empty selected tags list
            SelectedTagsDisplay.ItemsSource = new List<string>();

            // Populate hours (0-23)
            for (int i = 0; i < 24; i++)
            {
                HourComboBox.Items.Add(i.ToString("D2"));
            }
            HourComboBox.SelectedIndex = 9; // Default 09:00

            // Populate minutes (0, 15, 30, 45)
            MinuteComboBox.Items.Add("00");
            MinuteComboBox.Items.Add("15");
            MinuteComboBox.Items.Add("30");
            MinuteComboBox.Items.Add("45");
            MinuteComboBox.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Validate name
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("Task name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validate task type
            if (TaskTypeComboBox.SelectedValue == null)
            {
                MessageBox.Show("Task type is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Create task entity
            var task = new TaskEntity
            {
                Id = Guid.NewGuid(),
                Name = NameTextBox.Text.Trim(),
                Description = DescriptionTextBox.Text.Trim(),
                TaskTypeId = (int)TaskTypeComboBox.SelectedValue,
                UrgencyLevel = (int)UrgencySlider.Value,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Set deadline date
            if (DeadlineDatePicker.SelectedDate.HasValue)
            {
                task.DeadlineDate = DeadlineDatePicker.SelectedDate.Value;

                // Set deadline time if specified
                if (HourComboBox.SelectedItem != null && MinuteComboBox.SelectedItem != null)
                {
                    var hour = int.Parse(HourComboBox.SelectedItem.ToString()!);
                    var minute = int.Parse(MinuteComboBox.SelectedItem.ToString()!);
                    task.DeadlineTime = new TimeSpan(hour, minute, 0);
                }
            }

            // Set coordinates if specified
            if (double.TryParse(ImportanceTextBox.Text, out var importance))
            {
                if (importance < 0 || importance > 100)
                {
                    MessageBox.Show("Importance must be between 0 and 100.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                task.Importance = importance;
            }

            if (double.TryParse(ComplexityTextBox.Text, out var complexity))
            {
                if (complexity < 0 || complexity > 100)
                {
                    MessageBox.Show("Complexity must be between 0 and 100.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                task.Complexity = complexity;
            }

            // Set tags from selected tags display
            task.Tags = SelectedTagsDisplay.ItemsSource?.Cast<string>().ToList() ?? new List<string>();

            // Create task
            CreatedTask = await _taskManager.CreateTaskAsync(task);

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error creating task: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private async void AddTagToTaskButton_Click(object sender, RoutedEventArgs e)
    {
        var tagName = AvailableTagsComboBox.Text?.Trim();
        
        if (string.IsNullOrWhiteSpace(tagName))
        {
            MessageBox.Show("Please enter or select a tag name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            // Check if tag already added to task
            var currentTags = SelectedTagsDisplay.ItemsSource?.Cast<string>().ToList() ?? new List<string>();
            if (currentTags.Contains(tagName))
            {
                MessageBox.Show("This tag is already added to the task.", "Duplicate Tag", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Create tag in database if it doesn't exist
            var allTags = await _tagManager.GetAllTagsAsync();
            if (!allTags.Any(t => t.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase)))
            {
                await _tagManager.CreateTagAsync(tagName);
                
                // Refresh available tags dropdown
                var updatedTags = await _tagManager.GetAllTagsAsync();
                AvailableTagsComboBox.ItemsSource = updatedTags.Select(t => t.Name).ToList();
            }

            // Add tag to task's selected tags
            currentTags.Add(tagName);
            SelectedTagsDisplay.ItemsSource = null;
            SelectedTagsDisplay.ItemsSource = currentTags;

            // Clear input
            AvailableTagsComboBox.Text = string.Empty;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error adding tag: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RemoveSelectedTagButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button && button.Tag is string tagName)
        {
            var currentTags = SelectedTagsDisplay.ItemsSource?.Cast<string>().ToList() ?? new List<string>();
            currentTags.Remove(tagName);
            
            // Refresh display
            SelectedTagsDisplay.ItemsSource = null;
            SelectedTagsDisplay.ItemsSource = currentTags;
        }
    }
}
