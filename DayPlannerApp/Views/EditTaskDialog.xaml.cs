using System;
using System.Linq;
using System.Windows;
using DayPlannerApp.Models;
using DayPlannerApp.Services;

namespace DayPlannerApp.Views;

public partial class EditTaskDialog : Window
{
    private readonly ITaskManager _taskManager;
    private readonly ITaskTypeManager _taskTypeManager;
    private readonly ITagManager _tagManager;
    private readonly TaskEntity _task;
    
    public TaskEntity? UpdatedTask { get; private set; }

    public EditTaskDialog(
        TaskEntity task,
        ITaskManager taskManager,
        ITaskTypeManager taskTypeManager,
        ITagManager tagManager)
    {
        InitializeComponent();
        
        _task = task ?? throw new ArgumentNullException(nameof(task));
        _taskManager = taskManager ?? throw new ArgumentNullException(nameof(taskManager));
        _taskTypeManager = taskTypeManager ?? throw new ArgumentNullException(nameof(taskTypeManager));
        _tagManager = tagManager ?? throw new ArgumentNullException(nameof(tagManager));
        
        Loaded += EditTaskDialog_Loaded;
    }

    private async void EditTaskDialog_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // Load task types
            var taskTypes = await _taskTypeManager.GetAllTaskTypesAsync();
            TaskTypeComboBox.ItemsSource = taskTypes;
            TaskTypeComboBox.SelectedValue = _task.TaskTypeId;

            // Load tags
            var allTags = await _tagManager.GetAllTagsAsync();
            var tagNames = allTags.Select(t => t.Name).ToList();
            TagsListBox.ItemsSource = tagNames;

            // Populate hours (0-23)
            for (int i = 0; i < 24; i++)
            {
                HourComboBox.Items.Add(i.ToString("D2"));
            }

            // Populate minutes (0, 15, 30, 45)
            MinuteComboBox.Items.Add("00");
            MinuteComboBox.Items.Add("15");
            MinuteComboBox.Items.Add("30");
            MinuteComboBox.Items.Add("45");

            // Populate existing task data
            NameTextBox.Text = _task.Name;
            DescriptionTextBox.Text = _task.Description;
            UrgencySlider.Value = _task.UrgencyLevel;
            
            if (_task.DeadlineDate.HasValue)
            {
                DeadlineDatePicker.SelectedDate = _task.DeadlineDate.Value;
            }

            if (_task.DeadlineTime.HasValue)
            {
                HourComboBox.SelectedItem = _task.DeadlineTime.Value.Hours.ToString("D2");
                var minute = _task.DeadlineTime.Value.Minutes;
                MinuteComboBox.SelectedItem = minute.ToString("D2");
            }
            else
            {
                HourComboBox.SelectedIndex = 9; // Default 09:00
                MinuteComboBox.SelectedIndex = 0;
            }

            if (_task.Importance.HasValue)
            {
                ImportanceTextBox.Text = _task.Importance.Value.ToString("F0");
            }

            if (_task.Complexity.HasValue)
            {
                ComplexityTextBox.Text = _task.Complexity.Value.ToString("F0");
            }

            // Select existing tags
            foreach (var tag in _task.Tags)
            {
                var index = tagNames.IndexOf(tag);
                if (index >= 0)
                {
                    TagsListBox.SelectedItems.Add(tag);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
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

            // Update task entity
            _task.Name = NameTextBox.Text.Trim();
            _task.Description = DescriptionTextBox.Text.Trim();
            _task.TaskTypeId = (int)TaskTypeComboBox.SelectedValue;
            _task.UrgencyLevel = (int)UrgencySlider.Value;
            _task.UpdatedAt = DateTime.UtcNow;

            // Set deadline date
            if (DeadlineDatePicker.SelectedDate.HasValue)
            {
                _task.DeadlineDate = DeadlineDatePicker.SelectedDate.Value;

                // Set deadline time if specified
                if (HourComboBox.SelectedItem != null && MinuteComboBox.SelectedItem != null)
                {
                    var hour = int.Parse(HourComboBox.SelectedItem.ToString()!);
                    var minute = int.Parse(MinuteComboBox.SelectedItem.ToString()!);
                    _task.DeadlineTime = new TimeSpan(hour, minute, 0);
                }
            }
            else
            {
                _task.DeadlineDate = null;
                _task.DeadlineTime = null;
            }

            // Set coordinates if specified
            if (double.TryParse(ImportanceTextBox.Text, out var importance))
            {
                if (importance < 0 || importance > 100)
                {
                    MessageBox.Show("Importance must be between 0 and 100.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                _task.Importance = importance;
            }
            else
            {
                _task.Importance = null;
            }

            if (double.TryParse(ComplexityTextBox.Text, out var complexity))
            {
                if (complexity < 0 || complexity > 100)
                {
                    MessageBox.Show("Complexity must be between 0 and 100.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                _task.Complexity = complexity;
            }
            else
            {
                _task.Complexity = null;
            }

            // Set tags
            var selectedTags = TagsListBox.SelectedItems.Cast<string>().ToList();
            _task.Tags = selectedTags;

            // Update task
            UpdatedTask = await _taskManager.UpdateTaskAsync(_task);

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error updating task: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private async void AddTagButton_Click(object sender, RoutedEventArgs e)
    {
        var tagName = Microsoft.VisualBasic.Interaction.InputBox("Enter new tag name:", "Add Tag", "");
        
        if (!string.IsNullOrWhiteSpace(tagName))
        {
            try
            {
                await _tagManager.CreateTagAsync(tagName.Trim());
                
                // Reload tags
                var allTags = await _tagManager.GetAllTagsAsync();
                var tagNames = allTags.Select(t => t.Name).ToList();
                TagsListBox.ItemsSource = tagNames;
                
                // Select the new tag
                TagsListBox.SelectedItems.Add(tagName.Trim());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding tag: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void RemoveTagButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button && button.Tag is string tagName)
        {
            TagsListBox.SelectedItems.Remove(tagName);
        }
    }
}
