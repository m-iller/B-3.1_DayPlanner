using System.Threading.Tasks;
using DayPlannerApp.Models;

namespace DayPlannerApp.Repositories;

/// <summary>
/// Repository interface for IPC authentication token persistence.
/// </summary>
public interface IIPCAuthTokenRepository
{
    /// <summary>
    /// Inserts a new authentication token.
    /// </summary>
    Task InsertTokenAsync(IPCAuthToken token);

    /// <summary>
    /// Retrieves a token by its value.
    /// </summary>
    Task<IPCAuthToken?> GetTokenAsync(string token);

    /// <summary>
    /// Updates the last used timestamp for a token.
    /// </summary>
    Task UpdateLastUsedAsync(string token);

    /// <summary>
    /// Checks if any tokens exist in the database.
    /// </summary>
    Task<bool> AnyTokensExistAsync();
}
