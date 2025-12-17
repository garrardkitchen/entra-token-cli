using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace EntraTokenCli.Storage;

/// <summary>
/// Windows-specific secure storage implementation using DPAPI.
/// </summary>
public class WindowsSecureStorage : ISecureStorage
{
    private readonly string _storageDirectory;
    private readonly Dictionary<string, string> _cache = new();

    public WindowsSecureStorage()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new PlatformNotSupportedException("WindowsSecureStorage is only supported on Windows.");
        }

        _storageDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "entratool",
            "secure"
        );

        Directory.CreateDirectory(_storageDirectory);
    }

    public Task StoreAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        var sanitizedKey = SanitizeKey(key);
        var filePath = Path.Combine(_storageDirectory, $"{sanitizedKey}.dat");

        // Encrypt the value using DPAPI
        var plainTextBytes = Encoding.UTF8.GetBytes(value);
        var encryptedBytes = ProtectedData.Protect(
            plainTextBytes,
            optionalEntropy: null,
            scope: DataProtectionScope.CurrentUser
        );

        File.WriteAllBytes(filePath, encryptedBytes);
        _cache[key] = value;

        return Task.CompletedTask;
    }

    public Task<string?> RetrieveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(key, out var cachedValue))
        {
            return Task.FromResult<string?>(cachedValue);
        }

        var sanitizedKey = SanitizeKey(key);
        var filePath = Path.Combine(_storageDirectory, $"{sanitizedKey}.dat");

        if (!File.Exists(filePath))
        {
            return Task.FromResult<string?>(null);
        }

        try
        {
            var encryptedBytes = File.ReadAllBytes(filePath);
            var decryptedBytes = ProtectedData.Unprotect(
                encryptedBytes,
                optionalEntropy: null,
                scope: DataProtectionScope.CurrentUser
            );

            var value = Encoding.UTF8.GetString(decryptedBytes);
            _cache[key] = value;
            return Task.FromResult<string?>(value);
        }
        catch (CryptographicException)
        {
            // Data may be corrupted or encrypted by another user
            return Task.FromResult<string?>(null);
        }
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        var sanitizedKey = SanitizeKey(key);
        var filePath = Path.Combine(_storageDirectory, $"{sanitizedKey}.dat");

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        _cache.Remove(key);

        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_cache.ContainsKey(key))
        {
            return Task.FromResult(true);
        }

        var sanitizedKey = SanitizeKey(key);
        var filePath = Path.Combine(_storageDirectory, $"{sanitizedKey}.dat");
        return Task.FromResult(File.Exists(filePath));
    }

    private static string SanitizeKey(string key)
    {
        // Convert key to a safe filename using SHA256 hash
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var hashBytes = SHA256.HashData(keyBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
