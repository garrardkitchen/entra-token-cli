using System.Text.Json.Serialization;

namespace EntraTokenCli.Configuration;

/// <summary>
/// Represents a saved authentication profile.
/// </summary>
public record AuthProfile
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    [JsonPropertyName("clientId")]
    public string ClientId { get; set; } = string.Empty;

    [JsonPropertyName("scopes")]
    public List<string> Scopes { get; set; } = new();

    [JsonPropertyName("resource")]
    public string? Resource { get; set; }

    [JsonPropertyName("authMethod")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AuthenticationMethod AuthMethod { get; set; } = AuthenticationMethod.ClientSecret;

    [JsonPropertyName("redirectUri")]
    public string? RedirectUri { get; set; }

    [JsonPropertyName("certificatePath")]
    public string? CertificatePath { get; set; }

    [JsonPropertyName("cacheCertificatePassword")]
    public bool CacheCertificatePassword { get; set; } = false;

    [JsonPropertyName("defaultFlow")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OAuth2Flow? DefaultFlow { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Authentication methods supported by the CLI.
/// </summary>
public enum AuthenticationMethod
{
    ClientSecret,
    Certificate,
    PasswordlessCertificate
}

/// <summary>
/// OAuth2 flows supported by the CLI.
/// </summary>
public enum OAuth2Flow
{
    AuthorizationCode,
    ClientCredentials,
    DeviceCode,
    InteractiveBrowser
}
