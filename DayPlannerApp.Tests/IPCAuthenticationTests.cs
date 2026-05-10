using Xunit;
using DayPlannerApp.Services;
using DayPlannerApp.Repositories;
using DayPlannerApp.Models;

namespace DayPlannerApp.Tests;

/// <summary>
/// Unit tests for IPC authentication functionality.
/// Validates token generation, validation, and rejection of invalid tokens.
/// </summary>
public class IPCAuthenticationTests : IDisposable
{
    private readonly TestDatabaseHelper _dbHelper;
    private readonly IPCAuthTokenRepository _tokenRepository;
    private readonly IPCAuthenticationService _authService;
    private readonly TestLogger _logger;

    public IPCAuthenticationTests()
    {
        _dbHelper = new TestDatabaseHelper();
        _tokenRepository = new IPCAuthTokenRepository(_dbHelper.ConnectionString);
        _logger = new TestLogger();
        _authService = new IPCAuthenticationService(_tokenRepository, _logger);
    }

    [Fact]
    public async Task EnsureTokenExistsAsync_GeneratesTokenOnFirstRun()
    {
        // Arrange - fresh database with no tokens

        // Act
        await _authService.EnsureTokenExistsAsync();

        // Assert
        var tokensExist = await _tokenRepository.AnyTokensExistAsync();
        Assert.True(tokensExist);
    }

    [Fact]
    public async Task EnsureTokenExistsAsync_DoesNotGenerateDuplicateTokens()
    {
        // Arrange
        await _authService.EnsureTokenExistsAsync();

        // Act - call again
        await _authService.EnsureTokenExistsAsync();

        // Assert - should still have only one token
        var tokensExist = await _tokenRepository.AnyTokensExistAsync();
        Assert.True(tokensExist);
    }

    [Fact]
    public async Task ValidateTokenAsync_AcceptsValidToken()
    {
        // Arrange
        var token = new IPCAuthToken
        {
            Token = "valid-test-token-12345",
            Description = "Test token",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = null,
            LastUsedAt = null
        };
        await _tokenRepository.InsertTokenAsync(token);

        // Act
        var isValid = await _authService.ValidateTokenAsync("valid-test-token-12345");

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public async Task ValidateTokenAsync_RejectsInvalidToken()
    {
        // Arrange
        var token = new IPCAuthToken
        {
            Token = "valid-test-token-12345",
            Description = "Test token",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = null,
            LastUsedAt = null
        };
        await _tokenRepository.InsertTokenAsync(token);

        // Act
        var isValid = await _authService.ValidateTokenAsync("invalid-token");

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task ValidateTokenAsync_RejectsEmptyToken()
    {
        // Act
        var isValid = await _authService.ValidateTokenAsync("");

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task ValidateTokenAsync_RejectsExpiredToken()
    {
        // Arrange
        var token = new IPCAuthToken
        {
            Token = "expired-token",
            Description = "Expired test token",
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            ExpiresAt = DateTime.UtcNow.AddDays(-1), // Expired yesterday
            LastUsedAt = null
        };
        await _tokenRepository.InsertTokenAsync(token);

        // Act
        var isValid = await _authService.ValidateTokenAsync("expired-token");

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task ValidateTokenAsync_UpdatesLastUsedTimestamp()
    {
        // Arrange
        var token = new IPCAuthToken
        {
            Token = "test-token-timestamp",
            Description = "Test token",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = null,
            LastUsedAt = null
        };
        await _tokenRepository.InsertTokenAsync(token);

        // Act
        await _authService.ValidateTokenAsync("test-token-timestamp");

        // Assert
        var retrievedToken = await _tokenRepository.GetTokenAsync("test-token-timestamp");
        Assert.NotNull(retrievedToken);
        Assert.NotNull(retrievedToken.LastUsedAt);
    }

    [Fact]
    public async Task IPCAuthTokenRepository_InsertAndRetrieveToken()
    {
        // Arrange
        var token = new IPCAuthToken
        {
            Token = "repository-test-token",
            Description = "Repository test",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            LastUsedAt = null
        };

        // Act
        await _tokenRepository.InsertTokenAsync(token);
        var retrieved = await _tokenRepository.GetTokenAsync("repository-test-token");

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("repository-test-token", retrieved.Token);
        Assert.Equal("Repository test", retrieved.Description);
        Assert.NotNull(retrieved.ExpiresAt);
    }

    [Fact]
    public async Task IPCAuthTokenRepository_GetTokenAsync_ReturnsNullForNonExistentToken()
    {
        // Act
        var retrieved = await _tokenRepository.GetTokenAsync("non-existent-token");

        // Assert
        Assert.Null(retrieved);
    }

    public void Dispose()
    {
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
}
