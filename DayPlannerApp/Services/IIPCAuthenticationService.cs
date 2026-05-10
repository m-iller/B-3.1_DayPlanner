using System.Threading.Tasks;

namespace DayPlannerApp.Services;

/// <summary>
/// Service interface for IPC authentication operations.
/// </summary>
public interface IIPCAuthenticationService
{
    /// <summary>
    /// Ensures a shared secret token exists, generating one if needed.
    /// Should be called on application startup.
    /// </summary>
    Task EnsureTokenExistsAsync();

    /// <summary>
    /// Validates an authentication token.
    /// </summary>
    /// <returns>True if token is valid and not expired, false otherwise.</returns>
    Task<bool> ValidateTokenAsync(string token);

    /// <summary>
    /// Gets the current authentication token for display/configuration purposes.
    /// </summary>
    Task<string?> GetCurrentTokenAsync();
}
