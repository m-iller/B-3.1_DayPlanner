namespace DayPlannerApp.Models;

/// <summary>
/// Represents an IPC response message to external applications.
/// </summary>
public class IPCResponse
{
    /// <summary>
    /// Request identifier for correlation with the original request.
    /// </summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// HTTP-style status code (200, 400, 401, 404, 500, etc.).
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Response body containing operation results or data.
    /// </summary>
    public Dictionary<string, object>? Body { get; set; }

    /// <summary>
    /// Error message if the operation failed.
    /// </summary>
    public string? Error { get; set; }
}
