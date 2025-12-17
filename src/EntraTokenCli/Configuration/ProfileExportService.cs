using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EntraTokenCli.Storage;

namespace EntraTokenCli.Configuration;

/// <summary>
/// Service for exporting and importing profiles with optional encryption.
/// </summary>
public class ProfileExportService
{
    private readonly ConfigService _configService;
    private readonly ISecureStorage _secureStorage;

    public ProfileExportService(ConfigService configService, ISecureStorage secureStorage)
    {
        _configService = configService;
        _secureStorage = secureStorage;
    }

    /// <summary>
    /// Exports a profile to an encrypted JSON file.
    /// </summary>
    public async Task<string> ExportProfileAsync(
        string profileName,
        string passphrase,
        bool includeSecrets,
        CancellationToken cancellationToken = default)
    {
        var profile = _configService.GetProfile(profileName);
        if (profile == null)
        {
            throw new InvalidOperationException($"Profile '{profileName}' not found.");
        }

        var exportData = new ProfileExportData
        {
            Profile = profile,
            ExportedAt = DateTimeOffset.UtcNow
        };

        if (includeSecrets)
        {
            exportData.ClientSecret = await _configService.GetClientSecretAsync(profileName, cancellationToken);
            
            if (profile.CacheCertificatePassword)
            {
                exportData.CertificatePassword = await _configService.GetCertificatePasswordAsync(
                    profileName, cancellationToken);
            }
        }

        var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions { WriteIndented = true });
        var encrypted = EncryptString(json, passphrase);

        return Convert.ToBase64String(encrypted);
    }

    /// <summary>
    /// Imports a profile from an encrypted JSON string.
    /// </summary>
    public async Task<AuthProfile> ImportProfileAsync(
        string encryptedData,
        string passphrase,
        string? newProfileName = null,
        CancellationToken cancellationToken = default)
    {
        byte[] encryptedBytes;
        try
        {
            encryptedBytes = Convert.FromBase64String(encryptedData);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException("Invalid encrypted data format.");
        }

        string json;
        try
        {
            json = DecryptString(encryptedBytes, passphrase);
        }
        catch (CryptographicException)
        {
            throw new InvalidOperationException("Failed to decrypt profile. Check the passphrase.");
        }

        var exportData = JsonSerializer.Deserialize<ProfileExportData>(json);
        if (exportData?.Profile == null)
        {
            throw new InvalidOperationException("Invalid profile data.");
        }

        var profile = exportData.Profile;

        // Use new name if provided, or keep original
        if (!string.IsNullOrWhiteSpace(newProfileName))
        {
            profile.Name = newProfileName;
        }

        // Check if profile already exists
        if (_configService.GetProfile(profile.Name) != null)
        {
            throw new InvalidOperationException(
                $"Profile '{profile.Name}' already exists. Choose a different name.");
        }

        // Save the profile
        await _configService.SaveProfileAsync(profile, cancellationToken);

        // Import secrets if present
        if (!string.IsNullOrWhiteSpace(exportData.ClientSecret))
        {
            await _configService.StoreClientSecretAsync(
                profile.Name, exportData.ClientSecret, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(exportData.CertificatePassword))
        {
            await _configService.StoreCertificatePasswordAsync(
                profile.Name, exportData.CertificatePassword, cancellationToken);
        }

        return profile;
    }

    /// <summary>
    /// Encrypts a string using AES-256 with a passphrase.
    /// </summary>
    private static byte[] EncryptString(string plainText, string passphrase)
    {
        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        // Derive key from passphrase using PBKDF2
        var salt = RandomNumberGenerator.GetBytes(32);
        var key = Rfc2898DeriveBytes.Pbkdf2(
            passphrase, salt, 100000, HashAlgorithmName.SHA256, 32);
        
        aes.Key = key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // Combine salt + IV + encrypted data
        var result = new byte[salt.Length + aes.IV.Length + encryptedBytes.Length];
        Buffer.BlockCopy(salt, 0, result, 0, salt.Length);
        Buffer.BlockCopy(aes.IV, 0, result, salt.Length, aes.IV.Length);
        Buffer.BlockCopy(encryptedBytes, 0, result, salt.Length + aes.IV.Length, encryptedBytes.Length);

        return result;
    }

    /// <summary>
    /// Decrypts a string using AES-256 with a passphrase.
    /// </summary>
    private static string DecryptString(byte[] encryptedData, string passphrase)
    {
        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        // Extract salt, IV, and encrypted data
        var salt = new byte[32];
        var iv = new byte[16];
        var encrypted = new byte[encryptedData.Length - 48];

        Buffer.BlockCopy(encryptedData, 0, salt, 0, 32);
        Buffer.BlockCopy(encryptedData, 32, iv, 0, 16);
        Buffer.BlockCopy(encryptedData, 48, encrypted, 0, encrypted.Length);

        // Derive key from passphrase
        var key = Rfc2898DeriveBytes.Pbkdf2(
            passphrase, salt, 100000, HashAlgorithmName.SHA256, 32);
        
        aes.Key = key;
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        var decryptedBytes = decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);

        return Encoding.UTF8.GetString(decryptedBytes);
    }

    private class ProfileExportData
    {
        public AuthProfile Profile { get; set; } = null!;
        public string? ClientSecret { get; set; }
        public string? CertificatePassword { get; set; }
        public DateTimeOffset ExportedAt { get; set; }
    }
}
