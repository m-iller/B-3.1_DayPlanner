using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DayPlannerApp.Models;
using DayPlannerApp.Services;

namespace DayPlannerApp.ViewModels;

public class WeekViewViewModel : ViewModelBase
{
    private readonly ICalendarViewManager _calendarViewManager;

    private DateTime _weekStartDate = GetWeekStart(DateTime.Today);
    private ObservableCollection<DayTasksViewModel> _weekDays = new();

    public WeekViewViewModel(ICalendarViewManager calendarViewManager)
    {
        _calendarViewManager = calendarViewManager;

        LoadWeekCommand = new RelayCommand(async () => await LoadWeekAsync());
        NavigateToPreviousWeekCommand = new RelayCommand(NavigateToPreviousWeek);
        NavigateToNextWeekCommand = new RelayCommand(NavigateToNextWeek);
        NavigateToCurrentWeekCommand = new RelayCommand(NavigateToCurrentWeek);
    }

    public DateTime WeekStartDate
    {
        get => _weekStartDate;
        set
        {
            if (SetProperty(ref _weekStartDate, value))
            {
                _ = LoadWeekAsync();
            }
        }
    }

    public ObservableCollection<DayTasksViewModel> WeekDays
    {
        get => _weekDays;
        set => SetProperty(ref _weekDays, value);
    }

    public ICommand LoadWeekCommand { get; }
    public ICommand NavigateToPreviousWeekCommand { get; }
    public ICommand NavigateToNextWeekCommand { get; }
    public ICommand NavigateToCurrentWeekCommand { get; }

    public async Task LoadAsync()
    {
        await LoadWeekAsync();
    }

    private async Task LoadWeekAsync()
    {
        var tasks = await _calendarViewManager.GetWeekViewAsync(WeekStartDate);
        var tasksByDate = tasks.GroupBy(t => t.DeadlineDate?.Date ?? DateTime.MaxValue.Date)
                               .ToDictionary(g => g.Key, g => g.ToList());

        var weekDays = new List<DayTasksViewModel>();
        for (int i = 0; i < 7; i++)
        {
            var date = WeekStartDate.AddDays(i);
            var dayTasks = tasksByDate.ContainsKey(date) ? tasksByDate[date] : new List<TaskEntity>();
            weekDays.Add(new DayTasksViewModel
            {
                Date = date,
                Tasks = new ObservableCollection<TaskEntity>(dayTasks)
            });
        }

        WeekDays = new ObservableCollection<DayTasksViewModel>(weekDays);
    }

    private void NavigateToPreviousWeek()
    {
        WeekStartDate = WeekStartDate.AddDays(-7);
    }

    private void NavigateToNextWeek()
    {
        WeekStartDate = WeekStartDate.AddDays(7);
    }

    private void NavigateToCurrentWeek()
    {
        WeekStartDate = GetWeekStart(DateTime.Today);
    }

    private static DateTime GetWeekStart(DateTime date)
    {
        int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-diff).Date;
    }
}

public class DayTasksViewModel : ViewModelBase
{
    private DateTime _date;
    private ObservableCollection<TaskEntity> _tasks = new();

    public DateTime Date
    {
        get => _date;
        set => SetProperty(ref _date, value);
    }

    public ObservableCollection<TaskEntity> Tasks
    {
        get => _tasks;
        set => SetProperty(ref _tasks, value);
    }
}
