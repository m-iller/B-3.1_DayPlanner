using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using DayPlannerApp.Models;
using DayPlannerApp.Repositories;

namespace DayPlannerApp.Services;

/// <summary>
/// Service for IPC authentication token management.
/// Generates shared secret tokens and validates incoming requests.
/// </summary>
public class IPCAuthenticationService : IIPCAuthenticationService
{
    private readonly IIPCAuthTokenRepository _tokenRepository;
    private readonly ILogger _logger;
    private const int TOKEN_LENGTH_BYTES = 32;
    private string? _cachedToken;

    public IPCAuthenticationService(IIPCAuthTokenRepository tokenRepository, ILogger logger)
    {
        _tokenRepository = tokenRepository ?? throw new ArgumentNullException(nameof(tokenRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task EnsureTokenExistsAsync()
    {
        var tokensExist = await _tokenRepository.AnyTokensExistAsync();
        
        if (!tokensExist)
        {
            _logger.Info("No IPC authentication token found. Generating new token...");
            var token = GenerateSecureToken();
            
            var authToken = new IPCAuthToken
            {
                Token = token,
                Description = "Auto-generated shared secret for IPC authentication",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = null, // No expiration
                LastUsedAt = null
            };

            await _tokenRepository.InsertTokenAsync(authToken);
            _cachedToken = token;
            
            _logger.Info($"Generated new IPC authentication token: {token}");
        }
        else
        {
            _logger.Info("IPC authentication token already exists.");
        }
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.Debug("Token validation failed: empty token");
            return false;
        }

        var authToken = await _tokenRepository.GetTokenAsync(token);
        
        if (authToken == null)
        {
            _logger.Warning($"Token validation failed: token not found");
            return false;
        }

        // Check expiration
        if (authToken.ExpiresAt.HasValue && authToken.ExpiresAt.Value < DateTime.UtcNow)
        {
            _logger.Warning($"Token validation failed: token expired");
            return false;
        }

        // Update last used timestamp
        await _tokenRepository.UpdateLastUsedAsync(token);
        
        _logger.Debug("Token validation successful");
        return true;
    }

    public async Task<string?> GetCurrentTokenAsync()
    {
        if (_cachedToken != null)
            return _cachedToken;

        // Try to retrieve first token from database
        // Note: In production, you might want a more sophisticated approach
        // For now, we assume single token scenario
        var tokensExist = await _tokenRepository.AnyTokensExistAsync();
        if (!tokensExist)
            return null;

        // This is a simplified implementation
        // In a real scenario, you'd query for the active token
        return null;
    }

    private static string GenerateSecureToken()
    {
        var bytes = new byte[TOKEN_LENGTH_BYTES];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return Convert.ToBase64String(bytes);
    }
}
