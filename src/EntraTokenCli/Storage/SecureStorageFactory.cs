using System.Runtime.InteropServices;

namespace EntraAuthCli.Storage;

/// <summary>
/// Factory for creating platform-specific secure storage implementations.
/// </summary>
public static class SecureStorageFactory
{
    /// <summary>
    /// Creates an appropriate secure storage implementation for the current platform.
    /// </summary>
    /// <returns>A platform-specific ISecureStorage implementation.</returns>
    public static ISecureStorage Create()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new WindowsSecureStorage();
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return new MacOsSecureStorage();
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return new LinuxSecureStorage();
        }

        throw new PlatformNotSupportedException(
            $"Secure storage is not supported on this platform: {RuntimeInformation.OSDescription}"
        );
    }
}
