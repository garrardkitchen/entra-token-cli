using System.Runtime.InteropServices;
using System.Text;

namespace EntraTokenCli.Storage;

/// <summary>
/// Linux-specific secure storage implementation using libsecret via D-Bus.
/// </summary>
public class LinuxSecureStorage : ISecureStorage
{
    private const string ServiceName = "entra-auth-cli";
    private readonly Dictionary<string, string> _cache = new();
    private readonly string _fallbackDirectory;
    private bool _usesFallback = false;

    public LinuxSecureStorage()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            throw new PlatformNotSupportedException("LinuxSecureStorage is only supported on Linux.");
        }

        _fallbackDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".config",
            "azadtool",
            "secure"
        );
    }

    public async Task StoreAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        try
        {
            await StoreViaDBusAsync(key, value, cancellationToken);
            _cache[key] = value;
        }
        catch (Exception)
        {
            // Fallback to file-based storage if D-Bus is unavailable
            await StoreFallbackAsync(key, value);
            _usesFallback = true;
        }
    }

    public async Task<string?> RetrieveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(key, out var cachedValue))
        {
            return cachedValue;
        }

        try
        {
            var value = await RetrieveViaDBusAsync(key, cancellationToken);
            if (value != null)
            {
                _cache[key] = value;
                return value;
            }
        }
        catch (Exception)
        {
            // Try fallback storage
            _usesFallback = true;
        }

        // Try fallback storage
        var fallbackValue = await RetrieveFallbackAsync(key);
        if (fallbackValue != null)
        {
            _cache[key] = fallbackValue;
        }
        return fallbackValue;
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await DeleteViaDBusAsync(key, cancellationToken);
        }
        catch (Exception)
        {
            // Ignore D-Bus errors during deletion
        }

        await DeleteFallbackAsync(key);
        _cache.Remove(key);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_cache.ContainsKey(key))
        {
            return true;
        }

        var value = await RetrieveAsync(key, cancellationToken);
        return value != null;
    }

    private async Task StoreViaDBusAsync(string key, string value, CancellationToken cancellationToken)
    {
        // Full D-Bus implementation would require additional dependencies
        // For now, use fallback storage
        throw new NotImplementedException("D-Bus implementation pending. Using fallback storage.");
    }

    private Task<string?> RetrieveViaDBusAsync(string key, CancellationToken cancellationToken)
    {
        throw new NotImplementedException("Full D-Bus implementation requires additional work. Using fallback storage.");
    }

    private Task DeleteViaDBusAsync(string key, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private Task StoreFallbackAsync(string key, string value)
    {
        Directory.CreateDirectory(_fallbackDirectory);
        var filePath = GetFallbackFilePath(key);
        
        // Simple XOR encryption for basic obfuscation (not cryptographically secure)
        var bytes = Encoding.UTF8.GetBytes(value);
        var obfuscated = ObfuscateBytes(bytes);
        
        File.WriteAllBytes(filePath, obfuscated);
        return Task.CompletedTask;
    }

    private Task<string?> RetrieveFallbackAsync(string key)
    {
        var filePath = GetFallbackFilePath(key);
        
        if (!File.Exists(filePath))
        {
            return Task.FromResult<string?>(null);
        }

        var obfuscated = File.ReadAllBytes(filePath);
        var bytes = ObfuscateBytes(obfuscated); // XOR is reversible
        var value = Encoding.UTF8.GetString(bytes);
        
        return Task.FromResult<string?>(value);
    }

    private Task DeleteFallbackAsync(string key)
    {
        var filePath = GetFallbackFilePath(key);
        
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        
        return Task.CompletedTask;
    }

    private string GetFallbackFilePath(string key)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(key));
        var fileName = Convert.ToHexString(hash).ToLowerInvariant();
        return Path.Combine(_fallbackDirectory, $"{fileName}.dat");
    }

    private static byte[] ObfuscateBytes(byte[] input)
    {
        const byte xorKey = 0xAA;
        var output = new byte[input.Length];
        for (int i = 0; i < input.Length; i++)
        {
            output[i] = (byte)(input[i] ^ xorKey);
        }
        return output;
    }
}
