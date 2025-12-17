namespace EntraTokenCli.Storage;

/// <summary>
/// Interface for platform-specific secure storage implementations.
/// </summary>
public interface ISecureStorage
{
    /// <summary>
    /// Stores a value securely with the specified key.
    /// </summary>
    /// <param name="key">The unique identifier for the stored value.</param>
    /// <param name="value">The value to store securely.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StoreAsync(string key, string value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a securely stored value by key.
    /// </summary>
    /// <param name="key">The unique identifier for the stored value.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The stored value, or null if not found.</returns>
    Task<string?> RetrieveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a securely stored value by key.
    /// </summary>
    /// <param name="key">The unique identifier for the stored value.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a value exists for the specified key.
    /// </summary>
    /// <param name="key">The unique identifier to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the key exists, false otherwise.</returns>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
}
