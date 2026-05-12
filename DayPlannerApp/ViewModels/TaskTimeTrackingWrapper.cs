using System;
using System.Windows.Input;
using DayPlannerApp.Models;

namespace DayPlannerApp.ViewModels;

public class TaskTimeTrackingWrapper : ViewModelBase
{
    private readonly DayViewViewModel _parentViewModel;
    private bool _isTracking;
    private bool _isOnBreak;
    private bool _isCompleted;
    private TimeSpan _elapsedTime;
    private TimeSpan _breakTime;

    public TaskTimeTrackingWrapper(TaskEntity task, DayViewViewModel parentViewModel)
    {
        Task = task ?? throw new ArgumentNullException(nameof(task));
        _parentViewModel = parentViewModel ?? throw new ArgumentNullException(nameof(parentViewModel));
        TaskId = task.Id;
        _isCompleted = task.IsCompleted;

        StartTrackingCommand = new RelayCommand(
            async () => await _parentViewModel.StartTrackingAsync(TaskId),
            () => !IsTracking && !IsCompleted
        );

        PauseTrackingCommand = new RelayCommand(
            async () => await _parentViewModel.PauseTrackingAsync(TaskId),
            () => IsTracking && !IsOnBreak && !IsCompleted
        );

        ResumeTrackingCommand = new RelayCommand(
            async () => await _parentViewModel.ResumeTrackingAsync(TaskId),
            () => IsTracking && IsOnBreak && !IsCompleted
        );

        CompleteTrackingCommand = new RelayCommand(
            async () => await _parentViewModel.CompleteTrackingAsync(TaskId),
            () => IsTracking && !IsCompleted
        );

        MarkCompleteCommand = new RelayCommand(
            async () => await _parentViewModel.MarkTaskCompleteAsync(TaskId),
            () => !IsCompleted
        );

        MarkIncompleteCommand = new RelayCommand(
            async () => await _parentViewModel.MarkTaskIncompleteAsync(TaskId),
            () => IsCompleted
        );
    }

    public Guid TaskId { get; }
    public TaskEntity Task { get; }

    public bool IsTracking
    {
        get => _isTracking;
        set
        {
            if (SetProperty(ref _isTracking, value))
            {
                OnPropertyChanged(nameof(TrackingStateText));
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public bool IsOnBreak
    {
        get => _isOnBreak;
        set
        {
            if (SetProperty(ref _isOnBreak, value))
            {
                OnPropertyChanged(nameof(TrackingStateText));
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public bool IsCompleted
    {
        get => _isCompleted;
        set
        {
            if (SetProperty(ref _isCompleted, value))
            {
                Task.IsCompleted = value;
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public TimeSpan ElapsedTime
    {
        get => _elapsedTime;
        set
        {
            if (SetProperty(ref _elapsedTime, value))
            {
                OnPropertyChanged(nameof(DisplayTime));
            }
        }
    }

    public TimeSpan BreakTime
    {
        get => _breakTime;
        set => SetProperty(ref _breakTime, value);
    }

    public string DisplayTime => $"{(int)ElapsedTime.TotalHours:D2}:{ElapsedTime.Minutes:D2}:{ElapsedTime.Seconds:D2}";

    public string TrackingStateText
    {
        get
        {
            if (!IsTracking)
                return "Not Started";
            if (IsOnBreak)
                return "On Break";
            return "Tracking";
        }
    }

    public ICommand StartTrackingCommand { get; }
    public ICommand PauseTrackingCommand { get; }
    public ICommand ResumeTrackingCommand { get; }
    public ICommand CompleteTrackingCommand { get; }
    public ICommand MarkCompleteCommand { get; }
    public ICommand MarkIncompleteCommand { get; }
}
