using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace EntraTokenCli.Storage;

/// <summary>
/// macOS-specific secure storage implementation using Keychain.
/// </summary>
public class MacOsSecureStorage : ISecureStorage
{
    private const string ServiceName = "entratool";
    private readonly Dictionary<string, string> _cache = new();

    public MacOsSecureStorage()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            throw new PlatformNotSupportedException("MacOsSecureStorage is only supported on macOS.");
        }
    }

    public async Task StoreAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        // First, try to delete existing entry
        await DeleteAsync(key, cancellationToken);

        // Add new entry to Keychain
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "security",
                Arguments = $"add-generic-password -a \"{key}\" -s \"{ServiceName}\" -w \"{EscapeForShell(value)}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode == 0)
        {
            _cache[key] = value;
        }
        else
        {
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);
            throw new InvalidOperationException($"Failed to store value in Keychain: {error}");
        }
    }

    public async Task<string?> RetrieveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(key, out var cachedValue))
        {
            return cachedValue;
        }

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "security",
                Arguments = $"find-generic-password -a \"{key}\" -s \"{ServiceName}\" -w",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode == 0)
        {
            var value = output.TrimEnd('\n', '\r');
            _cache[key] = value;
            return value;
        }

        return null;
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "security",
                Arguments = $"delete-generic-password -a \"{key}\" -s \"{ServiceName}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        await process.WaitForExitAsync(cancellationToken);

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

    private static string EscapeForShell(string value)
    {
        // Escape special characters for shell
        return value.Replace("\"", "\\\"").Replace("$", "\\$").Replace("`", "\\`");
    }
}
