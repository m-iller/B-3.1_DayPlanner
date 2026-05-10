using DayPlannerApp.Models;

namespace DayPlannerApp.Services;

/// <summary>
/// Interface for handling IPC requests and routing them to business logic services.
/// </summary>
public interface IIPCRequestHandler
{
    /// <summary>
    /// Handles an IPC request and returns an appropriate response.
    /// </summary>
    /// <param name="request">The IPC request to handle.</param>
    /// <returns>An IPC response with status code and data or error message.</returns>
    Task<IPCResponse> HandleRequestAsync(IPCRequest request);
}
