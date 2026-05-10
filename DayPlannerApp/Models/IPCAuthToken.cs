using System;

namespace DayPlannerApp.Models;

/// <summary>
/// Represents an IPC authentication token for external application access.
/// </summary>
public class IPCAuthToken
{
    public string Token { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
}
