using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using DayPlannerApp.Models;
using DayPlannerApp.Services;

namespace DayPlannerApp.ViewModels;

public class CoordinateControllerViewModel : ViewModelBase
{
    private readonly ICoordinateController _coordinateController;
    private readonly ITaskManager _taskManager;

    private ObservableCollection<TaskCoordinateViewModel> _tasks = new();
    private double _minImportance = 0;
    private double _maxImportance = 100;
    private double _minComplexity = 0;
    private double _maxComplexity = 100;

    public CoordinateControllerViewModel(ICoordinateController coordinateController, ITaskManager taskManager)
    {
        _coordinateController = coordinateController;
        _taskManager = taskManager;

        LoadTasksCommand = new RelayCommand(async () => await LoadTasksAsync());
        UpdateTaskPositionCommand = new RelayCommand<TaskCoordinateViewModel>(async task => await UpdateTaskPositionAsync(task));
        FilterByRangeCommand = new RelayCommand(async () => await FilterByRangeAsync());
    }

    public ObservableCollection<TaskCoordinateViewModel> Tasks
    {
        get => _tasks;
        set => SetProperty(ref _tasks, value);
    }

    public double MinImportance
    {
        get => _minImportance;
        set => SetProperty(ref _minImportance, value);
    }

    public double MaxImportance
    {
        get => _maxImportance;
        set => SetProperty(ref _maxImportance, value);
    }

    public double MinComplexity
    {
        get => _minComplexity;
        set => SetProperty(ref _minComplexity, value);
    }

    public double MaxComplexity
    {
        get => _maxComplexity;
        set => SetProperty(ref _maxComplexity, value);
    }

    public ICommand LoadTasksCommand { get; }
    public ICommand UpdateTaskPositionCommand { get; }
    public ICommand FilterByRangeCommand { get; }

    public async Task LoadAsync()
    {
        await LoadTasksAsync();
    }

    private async Task LoadTasksAsync()
    {
        var spec = new TaskQuerySpec();
        var tasks = await _taskManager.QueryTasksAsync(spec);

        var taskViewModels = new ObservableCollection<TaskCoordinateViewModel>();
        foreach (var task in tasks)
        {
            if (task.Importance.HasValue && task.Complexity.HasValue)
            {
                taskViewModels.Add(new TaskCoordinateViewModel
                {
                    TaskId = task.Id,
                    Description = task.Description,
                    Importance = task.Importance.Value,
                    Complexity = task.Complexity.Value
                });
            }
        }

        Tasks = taskViewModels;
    }

    private async Task UpdateTaskPositionAsync(TaskCoordinateViewModel? task)
    {
        if (task == null)
            return;

        await _coordinateController.UpdateTaskCoordinatesAsync(task.TaskId, task.Importance, task.Complexity);
    }

    private async Task FilterByRangeAsync()
    {
        var tasks = await _coordinateController.GetTasksByCoordinateRangeAsync(
            MinImportance, MaxImportance, MinComplexity, MaxComplexity);

        var taskViewModels = new ObservableCollection<TaskCoordinateViewModel>();
        foreach (var task in tasks)
        {
            if (task.Importance.HasValue && task.Complexity.HasValue)
            {
                taskViewModels.Add(new TaskCoordinateViewModel
                {
                    TaskId = task.Id,
                    Description = task.Description,
                    Importance = task.Importance.Value,
                    Complexity = task.Complexity.Value
                });
            }
        }

        Tasks = taskViewModels;
    }
}

public class TaskCoordinateViewModel : ViewModelBase
{
    private Guid _taskId;
    private string _description = string.Empty;
    private double _importance;
    private double _complexity;

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

    public double Importance
    {
        get => _importance;
        set
        {
            if (value < 0 || value > 100)
                return;
            SetProperty(ref _importance, value);
        }
    }

    public double Complexity
    {
        get => _complexity;
        set
        {
            if (value < 0 || value > 100)
                return;
            SetProperty(ref _complexity, value);
        }
    }
}
