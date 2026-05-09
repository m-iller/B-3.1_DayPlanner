using System;
using System.Collections.Generic;

namespace DayPlannerApp.Models;

public class TaskEntity
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public int TaskTypeId { get; set; }
    public DateTime? DeadlineDate { get; set; }
    public TimeSpan? DeadlineTime { get; set; }
    public double? Importance { get; set; }
    public double? Complexity { get; set; }
    public int UrgencyLevel { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<string> Tags { get; set; } = new();
}
