using System;

namespace DayPlannerApp.Services;

public class DataChangeNotifier
{
    public event EventHandler<DataChangedEventArgs>? DataChanged;

    public void NotifyTaskChanged(Guid taskId, DataChangeType changeType)
    {
        DataChanged?.Invoke(this, new DataChangedEventArgs
        {
            EntityType = EntityType.Task,
            EntityId = taskId,
            ChangeType = changeType,
            Timestamp = DateTime.UtcNow
        });
    }

    public void NotifyTagChanged(string tagName, DataChangeType changeType)
    {
        DataChanged?.Invoke(this, new DataChangedEventArgs
        {
            EntityType = EntityType.Tag,
            EntityName = tagName,
            ChangeType = changeType,
            Timestamp = DateTime.UtcNow
        });
    }

    public void NotifyTimeTrackingChanged(Guid sessionId, DataChangeType changeType)
    {
        DataChanged?.Invoke(this, new DataChangedEventArgs
        {
            EntityType = EntityType.TimeTrackingSession,
            EntityId = sessionId,
            ChangeType = changeType,
            Timestamp = DateTime.UtcNow
        });
    }

    public void NotifyConfigurationChanged(string key, DataChangeType changeType)
    {
        DataChanged?.Invoke(this, new DataChangedEventArgs
        {
            EntityType = EntityType.Configuration,
            EntityName = key,
            ChangeType = changeType,
            Timestamp = DateTime.UtcNow
        });
    }
}

public class DataChangedEventArgs : EventArgs
{
    public EntityType EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public string? EntityName { get; set; }
    public DataChangeType ChangeType { get; set; }
    public DateTime Timestamp { get; set; }
}

public enum EntityType
{
    Task,
    Tag,
    TimeTrackingSession,
    Configuration,
    TaskType
}

public enum DataChangeType
{
    Created,
    Updated,
    Deleted
}
