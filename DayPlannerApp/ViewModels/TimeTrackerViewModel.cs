using System;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using DayPlannerApp.Services;

namespace DayPlannerApp.ViewModels;

public class TimeTrackerViewModel : ViewModelBase
{
    private readonly ITimeTracker _timeTracker;
    private readonly DispatcherTimer _timer;

    private Guid _taskId;
    private bool _isTracking;
    private bool _isOnBreak;
    private TimeSpan _elapsedTime;
    private TimeSpan _breakTime;
    private string _displayTime = "00:00:00";
    private string _breakDisplayTime = "00:00:00";

    public TimeTrackerViewModel(ITimeTracker timeTracker)
    {
        _timeTracker = timeTracker;

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += Timer_Tick;

        StartTrackingCommand = new RelayCommand(async () => await StartTrackingAsync(), CanStartTracking);
        StopTrackingCommand = new RelayCommand(async () => await StopTrackingAsync(), CanStopTracking);
        StartBreakCommand = new RelayCommand(async () => await StartBreakAsync(), CanStartBreak);
        EndBreakCommand = new RelayCommand(async () => await EndBreakAsync(), CanEndBreak);
    }

    public Guid TaskId
    {
        get => _taskId;
        set => SetProperty(ref _taskId, value);
    }

    public bool IsTracking
    {
        get => _isTracking;
        set => SetProperty(ref _isTracking, value);
    }

    public bool IsOnBreak
    {
        get => _isOnBreak;
        set => SetProperty(ref _isOnBreak, value);
    }

    public TimeSpan ElapsedTime
    {
        get => _elapsedTime;
        set
        {
            if (SetProperty(ref _elapsedTime, value))
            {
                DisplayTime = FormatTimeSpan(value);
            }
        }
    }

    public TimeSpan BreakTime
    {
        get => _breakTime;
        set
        {
            if (SetProperty(ref _breakTime, value))
            {
                BreakDisplayTime = FormatTimeSpan(value);
            }
        }
    }

    public string DisplayTime
    {
        get => _displayTime;
        set => SetProperty(ref _displayTime, value);
    }

    public string BreakDisplayTime
    {
        get => _breakDisplayTime;
        set => SetProperty(ref _breakDisplayTime, value);
    }

    public ICommand StartTrackingCommand { get; }
    public ICommand StopTrackingCommand { get; }
    public ICommand StartBreakCommand { get; }
    public ICommand EndBreakCommand { get; }

    public void SetTask(Guid taskId)
    {
        TaskId = taskId;
        UpdateDisplay();
    }

    private bool CanStartTracking() => !IsTracking && TaskId != Guid.Empty;

    private bool CanStopTracking() => IsTracking;

    private bool CanStartBreak() => IsTracking && !IsOnBreak;

    private bool CanEndBreak() => IsTracking && IsOnBreak;

    private async Task StartTrackingAsync()
    {
        await _timeTracker.StartTrackingAsync(TaskId);
        IsTracking = true;
        IsOnBreak = false;
        _timer.Start();
    }

    private async Task StopTrackingAsync()
    {
        await _timeTracker.StopTrackingAsync(TaskId);
        IsTracking = false;
        IsOnBreak = false;
        _timer.Stop();
        UpdateDisplay();
    }

    private async Task StartBreakAsync()
    {
        await _timeTracker.StartBreakAsync(TaskId);
        IsOnBreak = true;
    }

    private async Task EndBreakAsync()
    {
        await _timeTracker.EndBreakAsync(TaskId);
        IsOnBreak = false;
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        ElapsedTime = _timeTracker.GetElapsedTime(TaskId);
        BreakTime = _timeTracker.GetBreakTime(TaskId);
    }

    private static string FormatTimeSpan(TimeSpan time)
    {
        return $"{(int)time.TotalHours:D2}:{time.Minutes:D2}:{time.Seconds:D2}";
    }
}
