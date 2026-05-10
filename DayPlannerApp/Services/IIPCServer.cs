namespace DayPlannerApp.Services;

/// <summary>
/// Interface for IPC server that enables external application integration via named pipes.
/// </summary>
public interface IIPCServer
{
    /// <summary>
    /// Starts the IPC server with the specified pipe name.
    /// </summary>
    /// <param name="pipeName">Name of the named pipe to create.</param>
    Task StartAsync(string pipeName);

    /// <summary>
    /// Stops the IPC server and closes all connections.
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// Gets whether the IPC server is currently running.
    /// </summary>
    bool IsRunning { get; }
}
