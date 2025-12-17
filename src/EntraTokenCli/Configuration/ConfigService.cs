using System.Text.Json;
using System.Text.Json.Serialization;
using EntraTokenCli.Storage;

namespace EntraTokenCli.Configuration;

/// <summary>
/// Service for managing authentication profiles.
/// </summary>
public class ConfigService
{
    private readonly string _configDirectory;
    private readonly string _profilesFilePath;
    private readonly ISecureStorage _secureStorage;
    private readonly JsonSerializerOptions _jsonOptions;
    private List<AuthProfile> _profiles = new();

    public ConfigService(ISecureStorage secureStorage)
    {
        _secureStorage = secureStorage;
        
        _configDirectory = GetConfigDirectory();
        _profilesFilePath = Path.Combine(_configDirectory, "profiles.json");
        
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };

        Directory.CreateDirectory(_configDirectory);
    }

    /// <summary>
    /// Gets the platform-appropriate configuration directory.
    /// </summary>
    private static string GetConfigDirectory()
    {
        if (OperatingSystem.IsWindows())
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "entratool"
            );
        }
        
        // macOS and Linux
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".config",
            "entratool"
        );
    }

    /// <summary>
    /// Loads all profiles from disk.
    /// </summary>
    public async Task LoadProfilesAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_profilesFilePath))
        {
            _profiles = new List<AuthProfile>();
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_profilesFilePath, cancellationToken);
            _profiles = JsonSerializer.Deserialize<List<AuthProfile>>(json, _jsonOptions) ?? new();
        }
        catch (JsonException)
        {
            _profiles = new List<AuthProfile>();
        }
    }

    /// <summary>
    /// Saves all profiles to disk.
    /// </summary>
    public async Task SaveProfilesAsync(CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(_profiles, _jsonOptions);
        await File.WriteAllTextAsync(_profilesFilePath, json, cancellationToken);
    }

    /// <summary>
    /// Gets all profiles.
    /// </summary>
    public IReadOnlyList<AuthProfile> GetProfiles() => _profiles.AsReadOnly();

    /// <summary>
    /// Gets a profile by name.
    /// </summary>
    public AuthProfile? GetProfile(string name)
    {
        return _profiles.FirstOrDefault(p => 
            p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Creates or updates a profile.
    /// </summary>
    public async Task SaveProfileAsync(AuthProfile profile, CancellationToken cancellationToken = default)
    {
        var existing = GetProfile(profile.Name);
        
        if (existing != null)
        {
            _profiles.Remove(existing);
            profile.CreatedAt = existing.CreatedAt;
        }
        
        profile.UpdatedAt = DateTimeOffset.UtcNow;
        _profiles.Add(profile);
        
        await SaveProfilesAsync(cancellationToken);
    }

    /// <summary>
    /// Deletes a profile and its associated secrets.
    /// </summary>
    public async Task DeleteProfileAsync(string name, CancellationToken cancellationToken = default)
    {
        var profile = GetProfile(name);
        if (profile == null)
        {
            return;
        }

        _profiles.Remove(profile);
        
        // Delete associated secrets
        await _secureStorage.DeleteAsync($"profile:{name}:secret", cancellationToken);
        await _secureStorage.DeleteAsync($"profile:{name}:cert-password", cancellationToken);
        
        await SaveProfilesAsync(cancellationToken);
    }

    /// <summary>
    /// Stores a client secret for a profile.
    /// </summary>
    public async Task StoreClientSecretAsync(string profileName, string secret, 
        CancellationToken cancellationToken = default)
    {
        await _secureStorage.StoreAsync($"profile:{profileName}:secret", secret, cancellationToken);
    }

    /// <summary>
    /// Retrieves a client secret for a profile.
    /// </summary>
    public async Task<string?> GetClientSecretAsync(string profileName, 
        CancellationToken cancellationToken = default)
    {
        return await _secureStorage.RetrieveAsync($"profile:{profileName}:secret", cancellationToken);
    }

    /// <summary>
    /// Stores a certificate password for a profile.
    /// </summary>
    public async Task StoreCertificatePasswordAsync(string profileName, string password, 
        CancellationToken cancellationToken = default)
    {
        await _secureStorage.StoreAsync($"profile:{profileName}:cert-password", password, cancellationToken);
    }

    /// <summary>
    /// Retrieves a certificate password for a profile.
    /// </summary>
    public async Task<string?> GetCertificatePasswordAsync(string profileName, 
        CancellationToken cancellationToken = default)
    {
        return await _secureStorage.RetrieveAsync($"profile:{profileName}:cert-password", cancellationToken);
    }

    /// <summary>
    /// Validates a profile configuration when required for authentication.
    /// </summary>
    public async Task<(bool IsValid, List<string> Errors)> ValidateProfileAsync(
        AuthProfile profile, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        // Validate tenant ID format (GUID or domain)
        if (string.IsNullOrWhiteSpace(profile.TenantId))
        {
            errors.Add("Tenant ID is required.");
        }
        else if (!Guid.TryParse(profile.TenantId, out _) && 
                 !profile.TenantId.Contains('.', StringComparison.Ordinal))
        {
            errors.Add("Tenant ID must be a GUID or domain name (e.g., contoso.onmicrosoft.com).");
        }

        // Validate client ID format (GUID)
        if (string.IsNullOrWhiteSpace(profile.ClientId))
        {
            errors.Add("Client ID is required.");
        }
        else if (!Guid.TryParse(profile.ClientId, out _))
        {
            errors.Add("Client ID must be a valid GUID.");
        }

        // Validate scopes
        if (profile.Scopes.Count == 0 && string.IsNullOrWhiteSpace(profile.Resource))
        {
            errors.Add("At least one scope or resource must be specified.");
        }

        // Validate authentication method specific requirements
        switch (profile.AuthMethod)
        {
            case AuthenticationMethod.ClientSecret:
                var secret = await GetClientSecretAsync(profile.Name, cancellationToken);
                if (string.IsNullOrWhiteSpace(secret))
                {
                    errors.Add("Client secret is required for ClientSecret authentication method.");
                }
                break;

            case AuthenticationMethod.Certificate:
            case AuthenticationMethod.PasswordlessCertificate:
                if (string.IsNullOrWhiteSpace(profile.CertificatePath))
                {
                    errors.Add("Certificate path is required for certificate-based authentication.");
                }
                else if (!File.Exists(profile.CertificatePath))
                {
                    errors.Add($"Certificate file not found: {profile.CertificatePath}");
                }
                break;
        }

        return (errors.Count == 0, errors);
    }

    /// <summary>
    /// Gets the path to the last token file.
    /// </summary>
    public string GetLastTokenFilePath()
    {
        return Path.Combine(_configDirectory, "last-token.txt");
    }
}
