namespace DayPlannerApp.Models;

/// <summary>
/// Represents an IPC request message from external applications.
/// </summary>
public class IPCRequest
{
    /// <summary>
    /// Unique identifier for correlating requests with responses.
    /// </summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// HTTP-style method: GET, POST, PUT, DELETE.
    /// </summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// Resource path: /tasks, /tags, /time-tracking.
    /// </summary>
    public string Resource { get; set; } = string.Empty;

    /// <summary>
    /// Request body containing operation-specific data.
    /// </summary>
    public Dictionary<string, object>? Body { get; set; }

    /// <summary>
    /// Authentication token for request validation.
    /// </summary>
    public string? AuthToken { get; set; }
}
