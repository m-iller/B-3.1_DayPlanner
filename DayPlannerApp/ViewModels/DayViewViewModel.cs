using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using DayPlannerApp.Models;
using DayPlannerApp.Services;

namespace DayPlannerApp.ViewModels;

public class DayViewViewModel : ViewModelBase
{
    private readonly ICalendarViewManager _calendarViewManager;

    private DateTime _selectedDate = DateTime.Today;
    private ObservableCollection<TaskEntity> _tasksForDay = new();

    public DayViewViewModel(ICalendarViewManager calendarViewManager)
    {
        _calendarViewManager = calendarViewManager;

        LoadDayCommand = new RelayCommand(async () => await LoadDayAsync());
        NavigateToPreviousDayCommand = new RelayCommand(NavigateToPreviousDay);
        NavigateToNextDayCommand = new RelayCommand(NavigateToNextDay);
        NavigateToTodayCommand = new RelayCommand(NavigateToToday);
    }

    public DateTime SelectedDate
    {
        get => _selectedDate;
        set
        {
            if (SetProperty(ref _selectedDate, value))
            {
                _ = LoadDayAsync();
            }
        }
    }

    public ObservableCollection<TaskEntity> TasksForDay
    {
        get => _tasksForDay;
        set => SetProperty(ref _tasksForDay, value);
    }

    public ICommand LoadDayCommand { get; }
    public ICommand NavigateToPreviousDayCommand { get; }
    public ICommand NavigateToNextDayCommand { get; }
    public ICommand NavigateToTodayCommand { get; }

    public async Task LoadAsync()
    {
        await LoadDayAsync();
    }

    private async Task LoadDayAsync()
    {
        var tasks = await _calendarViewManager.GetDayViewAsync(SelectedDate);
        TasksForDay = new ObservableCollection<TaskEntity>(tasks);
    }

    private void NavigateToPreviousDay()
    {
        SelectedDate = SelectedDate.AddDays(-1);
    }

    private void NavigateToNextDay()
    {
        SelectedDate = SelectedDate.AddDays(1);
    }

    private void NavigateToToday()
    {
        SelectedDate = DateTime.Today;
    }
}
