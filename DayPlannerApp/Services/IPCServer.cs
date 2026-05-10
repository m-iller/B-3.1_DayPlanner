using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using DayPlannerApp.Models;

namespace DayPlannerApp.Services;

/// <summary>
/// IPC server implementation using named pipes for external application integration.
/// Listens for JSON-formatted requests and routes them to business logic.
/// </summary>
public class IPCServer : IIPCServer
{
    private readonly ILogger _logger;
    private readonly IIPCAuthenticationService _authService;
    private readonly IIPCRequestHandler _requestHandler;
    private NamedPipeServerStream? _pipeServer;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _listenerTask;
    private string _pipeName = string.Empty;

    public bool IsRunning { get; private set; }

    public IPCServer(ILogger logger, IIPCAuthenticationService authService, IIPCRequestHandler requestHandler)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _requestHandler = requestHandler ?? throw new ArgumentNullException(nameof(requestHandler));
    }

    /// <summary>
    /// Starts the IPC server with the specified pipe name.
    /// </summary>
    public async Task StartAsync(string pipeName)
    {
        if (string.IsNullOrWhiteSpace(pipeName))
        {
            throw new ArgumentException("Pipe name cannot be null or empty.", nameof(pipeName));
        }

        if (IsRunning)
        {
            _logger.Info($"IPC server already running on pipe: {_pipeName}");
            return;
        }

        _pipeName = pipeName;
        _cancellationTokenSource = new CancellationTokenSource();
        IsRunning = true;

        _logger.Info($"Starting IPC server on pipe: {_pipeName}");

        // Start listener task
        _listenerTask = Task.Run(() => ListenForConnectionsAsync(_cancellationTokenSource.Token));

        await Task.CompletedTask;
    }

    /// <summary>
    /// Stops the IPC server and closes all connections.
    /// </summary>
    public async Task StopAsync()
    {
        if (!IsRunning)
        {
            return;
        }

        _logger.Info("Stopping IPC server...");

        IsRunning = false;
        _cancellationTokenSource?.Cancel();

        // Close pipe server
        _pipeServer?.Close();
        _pipeServer?.Dispose();
        _pipeServer = null;

        // Wait for listener task to complete
        if (_listenerTask != null)
        {
            try
            {
                await _listenerTask;
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
        }

        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        _listenerTask = null;

        _logger.Info("IPC server stopped.");
    }

    /// <summary>
    /// Continuously listens for incoming pipe connections.
    /// </summary>
    private async Task ListenForConnectionsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Create new pipe server instance for each connection
                _pipeServer = new NamedPipeServerStream(
                    _pipeName,
                    PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                _logger.Debug($"Waiting for client connection on pipe: {_pipeName}");

                // Wait for client connection
                await _pipeServer.WaitForConnectionAsync(cancellationToken);

                _logger.Info("Client connected to IPC server.");

                // Handle the connection
                await HandleConnectionAsync(_pipeServer, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping server
                break;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in IPC listener: {ex.Message}", ex);
                
                // Clean up pipe server on error
                _pipeServer?.Dispose();
                _pipeServer = null;

                // Brief delay before retrying
                if (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, cancellationToken);
                }
            }
        }
    }

    /// <summary>
    /// Handles a single client connection.
    /// </summary>
    private async Task HandleConnectionAsync(NamedPipeServerStream pipeServer, CancellationToken cancellationToken)
    {
        try
        {
            // Read request from pipe
            using var reader = new StreamReader(pipeServer, Encoding.UTF8, leaveOpen: true);
            var requestJson = await reader.ReadToEndAsync();

            _logger.Debug($"Received IPC request: {requestJson}");

            IPCResponse response;

            try
            {
                // Deserialize request
                var request = JsonSerializer.Deserialize<IPCRequest>(requestJson);

                if (request == null)
                {
                    response = new IPCResponse
                    {
                        RequestId = string.Empty,
                        StatusCode = 400,
                        Error = "Invalid request format: request is null"
                    };
                }
                else
                {
                    // Validate authentication token
                    var isAuthenticated = await _authService.ValidateTokenAsync(request.AuthToken ?? string.Empty);
                    
                    if (!isAuthenticated)
                    {
                        _logger.Warning($"Unauthorized IPC request from client. RequestId: {request.RequestId}");
                        response = new IPCResponse
                        {
                            RequestId = request.RequestId,
                            StatusCode = 401,
                            Error = "Unauthorized: Invalid or missing authentication token"
                        };
                    }
                    else
                    {
                        // Route request to handler
                        response = await _requestHandler.HandleRequestAsync(request);
                    }
                }
            }
            catch (JsonException ex)
            {
                response = new IPCResponse
                {
                    RequestId = string.Empty,
                    StatusCode = 400,
                    Error = $"Invalid JSON format: {ex.Message}"
                };
            }

            // Serialize response
            var responseJson = JsonSerializer.Serialize(response);

            // Write response to pipe
            using var writer = new StreamWriter(pipeServer, Encoding.UTF8, leaveOpen: true);
            await writer.WriteAsync(responseJson);
            await writer.FlushAsync();

            _logger.Debug($"Sent IPC response: {responseJson}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error handling IPC connection: {ex.Message}", ex);
        }
        finally
        {
            // Disconnect and dispose pipe
            if (pipeServer.IsConnected)
            {
                pipeServer.Disconnect();
            }
            pipeServer.Dispose();
        }
    }
}
