using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DayPlannerApp.Models;
using DayPlannerApp.Services;

namespace DayPlannerApp.ViewModels;

public class TaskEditorViewModel : ViewModelBase
{
    private readonly ITaskManager _taskManager;
    private readonly ITagManager _tagManager;
    private readonly ICoordinateController _coordinateController;

    private Guid _taskId;
    private string _description = string.Empty;
    private int _taskTypeId = 1;
    private DateTime? _deadlineDate;
    private TimeSpan? _deadlineTime;
    private double? _importance;
    private double? _complexity;
    private int _urgencyLevel;
    private ObservableCollection<string> _selectedTags = new();
    private ObservableCollection<string> _availableTags = new();
    private bool _isEditMode;

    public TaskEditorViewModel(ITaskManager taskManager, ITagManager tagManager, ICoordinateController coordinateController)
    {
        _taskManager = taskManager;
        _tagManager = tagManager;
        _coordinateController = coordinateController;

        SaveCommand = new RelayCommand(async () => await SaveAsync(), CanSave);
        CancelCommand = new RelayCommand(Cancel);
        DeleteCommand = new RelayCommand(async () => await DeleteAsync(), () => IsEditMode);
        AddTagCommand = new RelayCommand<string>(AddTag);
        RemoveTagCommand = new RelayCommand<string>(RemoveTag);
        LoadAvailableTagsCommand = new RelayCommand(async () => await LoadAvailableTagsAsync());
    }

    public Guid TaskId
    {
        get => _taskId;
        set => SetProperty(ref _taskId, value);
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public int TaskTypeId
    {
        get => _taskTypeId;
        set => SetProperty(ref _taskTypeId, value);
    }

    public DateTime? DeadlineDate
    {
        get => _deadlineDate;
        set => SetProperty(ref _deadlineDate, value);
    }

    public TimeSpan? DeadlineTime
    {
        get => _deadlineTime;
        set => SetProperty(ref _deadlineTime, value);
    }

    public double? Importance
    {
        get => _importance;
        set
        {
            if (value.HasValue && (value.Value < 0 || value.Value > 100))
                return;
            SetProperty(ref _importance, value);
        }
    }

    public double? Complexity
    {
        get => _complexity;
        set
        {
            if (value.HasValue && (value.Value < 0 || value.Value > 100))
                return;
            SetProperty(ref _complexity, value);
        }
    }

    public int UrgencyLevel
    {
        get => _urgencyLevel;
        set => SetProperty(ref _urgencyLevel, value);
    }

    public ObservableCollection<string> SelectedTags
    {
        get => _selectedTags;
        set => SetProperty(ref _selectedTags, value);
    }

    public ObservableCollection<string> AvailableTags
    {
        get => _availableTags;
        set => SetProperty(ref _availableTags, value);
    }

    public bool IsEditMode
    {
        get => _isEditMode;
        set => SetProperty(ref _isEditMode, value);
    }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand AddTagCommand { get; }
    public ICommand RemoveTagCommand { get; }
    public ICommand LoadAvailableTagsCommand { get; }

    public event EventHandler? TaskSaved;
    public event EventHandler? TaskDeleted;
    public event EventHandler? Cancelled;

    public async Task LoadTaskAsync(Guid taskId)
    {
        var task = await _taskManager.GetTaskByIdAsync(taskId);
        if (task == null)
            return;

        TaskId = task.Id;
        Description = task.Description;
        TaskTypeId = task.TaskTypeId;
        DeadlineDate = task.DeadlineDate;
        DeadlineTime = task.DeadlineTime;
        Importance = task.Importance;
        Complexity = task.Complexity;
        UrgencyLevel = task.UrgencyLevel;
        SelectedTags = new ObservableCollection<string>(task.Tags);
        IsEditMode = true;
    }

    public void CreateNew()
    {
        TaskId = Guid.NewGuid();
        Description = string.Empty;
        TaskTypeId = 1;
        DeadlineDate = null;
        DeadlineTime = null;
        Importance = null;
        Complexity = null;
        UrgencyLevel = 0;
        SelectedTags = new ObservableCollection<string>();
        IsEditMode = false;
    }

    private async Task LoadAvailableTagsAsync()
    {
        var tags = await _tagManager.GetAllTagsAsync();
        AvailableTags = new ObservableCollection<string>(tags.Select(t => t.Name));
    }

    private bool CanSave()
    {
        return !string.IsNullOrWhiteSpace(Description);
    }

    private async Task SaveAsync()
    {
        var task = new TaskEntity
        {
            Id = TaskId,
            Description = Description,
            TaskTypeId = TaskTypeId,
            DeadlineDate = DeadlineDate,
            DeadlineTime = DeadlineTime,
            Importance = Importance,
            Complexity = Complexity,
            UrgencyLevel = UrgencyLevel,
            Tags = SelectedTags.ToList(),
            UpdatedAt = DateTime.UtcNow
        };

        if (IsEditMode)
        {
            await _taskManager.UpdateTaskAsync(task);
        }
        else
        {
            task.CreatedAt = DateTime.UtcNow;
            await _taskManager.CreateTaskAsync(task);
        }

        // Update coordinates if set
        if (Importance.HasValue && Complexity.HasValue)
        {
            await _coordinateController.UpdateTaskCoordinatesAsync(TaskId, Importance.Value, Complexity.Value);
        }

        // Update tags
        var existingTags = await _tagManager.GetTagsForTaskAsync(TaskId);
        var existingTagNames = existingTags.Select(t => t.Name).ToHashSet();

        // Add new tags
        foreach (var tag in SelectedTags.Where(t => !existingTagNames.Contains(t)))
        {
            await _tagManager.AssignTagToTaskAsync(TaskId, tag);
        }

        // Remove unselected tags
        foreach (var tag in existingTagNames.Where(t => !SelectedTags.Contains(t)))
        {
            await _tagManager.RemoveTagFromTaskAsync(TaskId, tag);
        }

        TaskSaved?.Invoke(this, EventArgs.Empty);
    }

    private void Cancel()
    {
        Cancelled?.Invoke(this, EventArgs.Empty);
    }

    private async Task DeleteAsync()
    {
        if (!IsEditMode)
            return;

        await _taskManager.DeleteTaskAsync(TaskId);
        TaskDeleted?.Invoke(this, EventArgs.Empty);
    }

    private void AddTag(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag) || SelectedTags.Contains(tag))
            return;

        SelectedTags.Add(tag);
    }

    private void RemoveTag(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return;

        SelectedTags.Remove(tag);
    }
}
