using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DayPlannerApp.Models;
using DayPlannerApp.Services;

namespace DayPlannerApp.ViewModels;

public class TaskListViewModel : ViewModelBase
{
    private readonly ITaskManager _taskManager;
    private readonly ITagManager _tagManager;

    private ObservableCollection<TaskEntity> _tasks = new();
    private TaskEntity? _selectedTask;
    private string _filterTag = string.Empty;
    private int? _filterTypeId;
    private DateTime? _filterStartDate;
    private DateTime? _filterEndDate;
    private bool _sortByUrgency;
    private bool _sortByDeadline;

    public TaskListViewModel(ITaskManager taskManager, ITagManager tagManager)
    {
        _taskManager = taskManager;
        _tagManager = tagManager;

        LoadTasksCommand = new RelayCommand(async () => await LoadTasksAsync());
        FilterByTagCommand = new RelayCommand<string>(async tag => await FilterByTagAsync(tag));
        FilterByTypeCommand = new RelayCommand<int?>(async typeId => await FilterByTypeAsync(typeId));
        FilterByDateRangeCommand = new RelayCommand(async () => await FilterByDateRangeAsync());
        SortByUrgencyCommand = new RelayCommand(SortByUrgency);
        SortByDeadlineCommand = new RelayCommand(SortByDeadline);
        ClearFiltersCommand = new RelayCommand(async () => await ClearFiltersAsync());
    }

    public ObservableCollection<TaskEntity> Tasks
    {
        get => _tasks;
        set => SetProperty(ref _tasks, value);
    }

    public TaskEntity? SelectedTask
    {
        get => _selectedTask;
        set => SetProperty(ref _selectedTask, value);
    }

    public string FilterTag
    {
        get => _filterTag;
        set => SetProperty(ref _filterTag, value);
    }

    public int? FilterTypeId
    {
        get => _filterTypeId;
        set => SetProperty(ref _filterTypeId, value);
    }

    public DateTime? FilterStartDate
    {
        get => _filterStartDate;
        set => SetProperty(ref _filterStartDate, value);
    }

    public DateTime? FilterEndDate
    {
        get => _filterEndDate;
        set => SetProperty(ref _filterEndDate, value);
    }

    public bool SortByUrgencyEnabled
    {
        get => _sortByUrgency;
        set => SetProperty(ref _sortByUrgency, value);
    }

    public bool SortByDeadlineEnabled
    {
        get => _sortByDeadline;
        set => SetProperty(ref _sortByDeadline, value);
    }

    public ICommand LoadTasksCommand { get; }
    public ICommand FilterByTagCommand { get; }
    public ICommand FilterByTypeCommand { get; }
    public ICommand FilterByDateRangeCommand { get; }
    public ICommand SortByUrgencyCommand { get; }
    public ICommand SortByDeadlineCommand { get; }
    public ICommand ClearFiltersCommand { get; }

    private async Task LoadTasksAsync()
    {
        var spec = new TaskQuerySpec();
        var tasks = await _taskManager.QueryTasksAsync(spec);
        Tasks = new ObservableCollection<TaskEntity>(tasks);
    }

    private async Task FilterByTagAsync(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            await LoadTasksAsync();
            return;
        }

        var tasks = await _tagManager.SearchTasksByTagsAsync(new[] { tag });
        Tasks = new ObservableCollection<TaskEntity>(tasks);
    }

    private async Task FilterByTypeAsync(int? typeId)
    {
        if (!typeId.HasValue)
        {
            await LoadTasksAsync();
            return;
        }

        var tasks = await _taskManager.GetTasksByTypeAsync(typeId.Value);
        Tasks = new ObservableCollection<TaskEntity>(tasks);
    }

    private async Task FilterByDateRangeAsync()
    {
        if (!FilterStartDate.HasValue || !FilterEndDate.HasValue)
        {
            await LoadTasksAsync();
            return;
        }

        var tasks = await _taskManager.GetTasksByDateRangeAsync(FilterStartDate.Value, FilterEndDate.Value);
        Tasks = new ObservableCollection<TaskEntity>(tasks);
    }

    private void SortByUrgency()
    {
        var sorted = Tasks.OrderByDescending(t => t.UrgencyLevel).ToList();
        Tasks = new ObservableCollection<TaskEntity>(sorted);
        SortByUrgencyEnabled = true;
        SortByDeadlineEnabled = false;
    }

    private void SortByDeadline()
    {
        var sorted = Tasks.OrderBy(t => t.DeadlineDate ?? DateTime.MaxValue).ToList();
        Tasks = new ObservableCollection<TaskEntity>(sorted);
        SortByDeadlineEnabled = true;
        SortByUrgencyEnabled = false;
    }

    private async Task ClearFiltersAsync()
    {
        FilterTag = string.Empty;
        FilterTypeId = null;
        FilterStartDate = null;
        FilterEndDate = null;
        SortByUrgencyEnabled = false;
        SortByDeadlineEnabled = false;
        await LoadTasksAsync();
    }
}
