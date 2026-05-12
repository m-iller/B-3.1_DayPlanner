using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using DayPlannerApp.Models;
using DayPlannerApp.Services;

namespace DayPlannerApp.ViewModels;

/// <summary>
/// ViewModel for application settings
/// </summary>
public class SettingsViewModel : ViewModelBase
{
    private readonly ITaskTypeManager _taskTypeManager;
    private readonly INotificationService _notificationService;
    private readonly ILogger _logger;
    private readonly List<int> _deletedTaskTypeIds;

    public ObservableCollection<TaskTypeViewModel> TaskTypes { get; }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand AddTaskTypeCommand { get; }
    public ICommand DeleteTaskTypeCommand { get; }

    public SettingsViewModel(
        ITaskTypeManager taskTypeManager,
        INotificationService notificationService,
        ILogger logger)
    {
        _taskTypeManager = taskTypeManager ?? throw new ArgumentNullException(nameof(taskTypeManager));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        TaskTypes = new ObservableCollection<TaskTypeViewModel>();
        _deletedTaskTypeIds = new List<int>();
        SaveCommand = new RelayCommand(async () => await SaveAsync());
        CancelCommand = new RelayCommand(Cancel);
        AddTaskTypeCommand = new RelayCommand(AddTaskType);
        DeleteTaskTypeCommand = new RelayCommand<int>(async (id) => await DeleteTaskTypeAsync(id));
    }

    public async Task LoadAsync()
    {
        try
        {
            _logger.Info("Loading settings");
            var taskTypes = await _taskTypeManager.GetAllTaskTypesAsync();
            
            TaskTypes.Clear();
            foreach (var taskType in taskTypes)
            {
                TaskTypes.Add(new TaskTypeViewModel(taskType));
            }
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to load settings", ex);
            _notificationService.ShowError("Failed to load settings. Please try again.");
        }
    }

    private async Task SaveAsync()
    {
        try
        {
            _logger.Info("Saving settings");
            
            // Handle new task types (need to be inserted)
            foreach (var taskTypeVm in TaskTypes.Where(t => t.IsNew))
            {
                var taskType = new TaskType
                {
                    Id = taskTypeVm.Id,
                    Name = taskTypeVm.Name,
                    ColorHex = "#808080"
                };
                await _taskTypeManager.CreateTaskTypeAsync(taskType);
                taskTypeVm.IsNew = false;
                taskTypeVm.IsModified = false;
            }
            
            // Handle modified task types
            foreach (var taskTypeVm in TaskTypes.Where(t => t.IsModified && !t.IsNew))
            {
                await _taskTypeManager.UpdateTaskTypeNameAsync(taskTypeVm.Id, taskTypeVm.Name);
                taskTypeVm.IsModified = false;
            }

            // Handle deleted task types (tracked separately)
            foreach (var deletedId in _deletedTaskTypeIds)
            {
                await _taskTypeManager.DeleteTaskTypeAsync(deletedId);
            }
            _deletedTaskTypeIds.Clear();

            _notificationService.ShowInfo("Settings saved successfully.");
            _logger.Info("Settings saved successfully");
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to save settings", ex);
            _notificationService.ShowError("Failed to save settings. Please try again.");
        }
    }

    private void Cancel()
    {
        _logger.Info("Settings cancelled");
        // Reload to discard changes
        _ = LoadAsync();
    }

    private void AddTaskType()
    {
        try
        {
            // Find next available ID
            var maxId = TaskTypes.Any() ? TaskTypes.Max(t => t.Id) : 0;
            var newId = maxId + 1;

            var newTaskType = new TaskType
            {
                Id = newId,
                Name = $"New Type {newId}",
                ColorHex = "#808080"
            };

            TaskTypes.Add(new TaskTypeViewModel(newTaskType) { IsModified = true, IsNew = true });
            _logger.Info($"Added new task type with ID {newId}");
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to add task type", ex);
            _notificationService.ShowError("Failed to add task type.");
        }
    }

    private async Task DeleteTaskTypeAsync(int taskTypeId)
    {
        try
        {
            // Prevent deleting if only one type remains
            if (TaskTypes.Count <= 1)
            {
                _notificationService.ShowWarning("Cannot delete the last task type.");
                return;
            }

            var taskTypeVm = TaskTypes.FirstOrDefault(t => t.Id == taskTypeId);
            if (taskTypeVm != null)
            {
                // Track for deletion on save
                if (!taskTypeVm.IsNew)
                {
                    _deletedTaskTypeIds.Add(taskTypeId);
                }
                
                TaskTypes.Remove(taskTypeVm);
                _logger.Info($"Deleted task type {taskTypeId}");
                _notificationService.ShowInfo("Task type deleted. Remember to save changes.");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to delete task type {taskTypeId}", ex);
            _notificationService.ShowError("Failed to delete task type.");
        }
    }
}

/// <summary>
/// ViewModel for individual task type configuration
/// </summary>
public class TaskTypeViewModel : ViewModelBase
{
    private string _name;
    private bool _isModified;
    private bool _isNew;

    public int Id { get; }
    
    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                IsModified = true;
                OnPropertyChanged();
            }
        }
    }

    public bool IsModified
    {
        get => _isModified;
        set
        {
            _isModified = value;
            OnPropertyChanged();
        }
    }

    public bool IsNew
    {
        get => _isNew;
        set
        {
            _isNew = value;
            OnPropertyChanged();
        }
    }

    public TaskTypeViewModel(TaskType taskType)
    {
        Id = taskType.Id;
        _name = taskType.Name;
        _isModified = false;
        _isNew = false;
    }
}
