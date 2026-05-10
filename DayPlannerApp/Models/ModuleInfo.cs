using System;

namespace DayPlannerApp.Models;

/// <summary>
/// Module metadata and state information
/// </summary>
public class ModuleInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AssemblyPath { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public bool IsLoaded { get; set; }
    public DateTime LoadedAt { get; set; }
}
