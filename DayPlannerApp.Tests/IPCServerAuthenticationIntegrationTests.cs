using Xunit;
using DayPlannerApp.Services;
using DayPlannerApp.Repositories;
using DayPlannerApp.Models;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace DayPlannerApp.Tests;

/// <summary>
/// Integration tests for IPC server authentication.
/// Validates end-to-end authentication flow including request rejection.
/// </summary>
public class IPCServerAuthenticationIntegrationTests : IDisposable
{
    private readonly TestDatabaseHelper _dbHelper;
    private readonly IPCAuthTokenRepository _tokenRepository;
    private readonly IPCAuthenticationService _authService;
    private readonly IPCServer _ipcServer;
    private readonly TestLogger _logger;
    private const string TEST_PIPE_NAME = "DayPlannerApp_Test_IPC";

    public IPCServerAuthenticationIntegrationTests()
    {
        _dbHelper = new TestDatabaseHelper();
        _tokenRepository = new IPCAuthTokenRepository(_dbHelper.ConnectionString);
        _logger = new TestLogger();
        _authService = new IPCAuthenticationService(_tokenRepository, _logger);
        
        // Create mock request handler for authentication tests
        var mockRequestHandler = new MockRequestHandler();
        _ipcServer = new IPCServer(_logger, _authService, mockRequestHandler);
    }

    [Fact]
    public async Task IPCServer_RejectsRequestWithoutToken()
    {
        // Arrange
        await _authService.EnsureTokenExistsAsync();
        await _ipcServer.StartAsync(TEST_PIPE_NAME);
        await Task.Delay(100); // Give server time to start

        // Act
        var response = await SendIPCRequestAsync(new IPCRequest
        {
            RequestId = Guid.NewGuid().ToString(),
            Method = "GET",
            Resource = "/tasks",
            AuthToken = null // No token
        });

        // Assert
        Assert.NotNull(response);
        Assert.Equal(401, response.StatusCode);
        Assert.Contains("Unauthorized", response.Error ?? "");

        // Cleanup
        await _ipcServer.StopAsync();
    }

    [Fact]
    public async Task IPCServer_RejectsRequestWithInvalidToken()
    {
        // Arrange
        await _authService.EnsureTokenExistsAsync();
        await _ipcServer.StartAsync(TEST_PIPE_NAME);
        await Task.Delay(100);

        // Act
        var response = await SendIPCRequestAsync(new IPCRequest
        {
            RequestId = Guid.NewGuid().ToString(),
            Method = "GET",
            Resource = "/tasks",
            AuthToken = "invalid-token-12345"
        });

        // Assert
        Assert.NotNull(response);
        Assert.Equal(401, response.StatusCode);
        Assert.Contains("Unauthorized", response.Error ?? "");

        // Cleanup
        await _ipcServer.StopAsync();
    }

    [Fact]
    public async Task IPCServer_AcceptsRequestWithValidToken()
    {
        // Arrange
        var validToken = "valid-test-token-xyz";
        await _tokenRepository.InsertTokenAsync(new IPCAuthToken
        {
            Token = validToken,
            Description = "Test token",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = null,
            LastUsedAt = null
        });

        await _ipcServer.StartAsync(TEST_PIPE_NAME);
        await Task.Delay(100);

        // Act
        var response = await SendIPCRequestAsync(new IPCRequest
        {
            RequestId = Guid.NewGuid().ToString(),
            Method = "GET",
            Resource = "/tasks",
            AuthToken = validToken
        });

        // Assert
        Assert.NotNull(response);
        Assert.NotEqual(401, response.StatusCode); // Should not be unauthorized
        // Note: Will be 501 (not implemented) since request handling isn't done yet

        // Cleanup
        await _ipcServer.StopAsync();
    }

    private async Task<IPCResponse> SendIPCRequestAsync(IPCRequest request)
    {
        using var client = new NamedPipeClientStream(".", TEST_PIPE_NAME, PipeDirection.InOut);
        await client.ConnectAsync(5000); // 5 second timeout

        // Send request
        var requestJson = JsonSerializer.Serialize(request);
        var requestBytes = Encoding.UTF8.GetBytes(requestJson);
        await client.WriteAsync(requestBytes, 0, requestBytes.Length);
        await client.FlushAsync();

        // Read response
        using var reader = new StreamReader(client, Encoding.UTF8);
        var responseJson = await reader.ReadToEndAsync();
        
        return JsonSerializer.Deserialize<IPCResponse>(responseJson) 
            ?? throw new InvalidOperationException("Failed to deserialize response");
    }

    public void Dispose()
    {
        _ipcServer?.StopAsync().Wait();
        _dbHelper?.Dispose();
    }

    private class TestLogger : ILogger
    {
        public void Debug(string message) { }
        public void Info(string message) { }
        public void Warning(string message) { }
        public void Error(string message, Exception? exception = null) { }
        public void Fatal(string message, Exception? exception = null) { }
    }

    private class MockRequestHandler : IIPCRequestHandler
    {
        public Task<IPCResponse> HandleRequestAsync(IPCRequest request)
        {
            return Task.FromResult(new IPCResponse
            {
                RequestId = request.RequestId,
                StatusCode = 200,
                Body = new Dictionary<string, object> { ["message"] = "Mock response" }
            });
        }
    }
}
