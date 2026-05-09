using System;
using System.Collections.Generic;

namespace DayPlannerApp.Models;

public class TaskQuerySpec
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<string>? Tags { get; set; }
    public int? TaskTypeId { get; set; }
    public int? MinUrgency { get; set; }
    public int? MaxUrgency { get; set; }
    public double? MinImportance { get; set; }
    public double? MaxImportance { get; set; }
    public double? MinComplexity { get; set; }
    public double? MaxComplexity { get; set; }
}
