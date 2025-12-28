using System.Security.Cryptography.X509Certificates;
using EntraAuthCli.Configuration;
using EntraAuthCli.Storage;
using Microsoft.Identity.Client;
using Spectre.Console;

namespace EntraAuthCli.Authentication;

/// <summary>
/// Result of an authentication operation.
/// </summary>
public class AuthenticationResult
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTimeOffset ExpiresOn { get; set; }
    public string[] Scopes { get; set; } = Array.Empty<string>();
    public string TokenType { get; set; } = "Bearer";
    public bool IsFromCache { get; set; }
}

/// <summary>
/// MSAL-based authentication service supporting multiple OAuth2 flows.
/// </summary>
public class MsalAuthService
{
    private readonly ConfigService _configService;
    private readonly ISecureStorage _secureStorage;
    private readonly Dictionary<string, IClientApplicationBase> _clientCache = new();

    public MsalAuthService(ConfigService configService, ISecureStorage secureStorage)
    {
        _configService = configService;
        _secureStorage = secureStorage;
    }

    /// <summary>
    /// Authenticates using the specified profile and flow.
    /// </summary>
    public async Task<AuthenticationResult> AuthenticateAsync(
        AuthProfile profile,
        OAuth2Flow flow,
        int? port = null,
        bool cacheCertPassword = false,
        X509Certificate2? preLoadedCertificate = null,
        CancellationToken cancellationToken = default)
    {
        // Validate profile before attempting authentication
        var (isValid, errors) = await _configService.ValidateProfileAsync(profile, cancellationToken);
        if (!isValid)
        {
            throw new InvalidOperationException(
                $"Profile validation failed:\n{string.Join("\n", errors)}");
        }

        return flow switch
        {
            OAuth2Flow.ClientCredentials => await AuthenticateClientCredentialsAsync(
                profile, cacheCertPassword, preLoadedCertificate, cancellationToken),
            OAuth2Flow.AuthorizationCode => await AuthenticateAuthorizationCodeAsync(
                profile, port, cacheCertPassword, preLoadedCertificate, cancellationToken),
            OAuth2Flow.DeviceCode => await AuthenticateDeviceCodeAsync(
                profile, cacheCertPassword, preLoadedCertificate, cancellationToken),
            OAuth2Flow.InteractiveBrowser => await AuthenticateInteractiveBrowserAsync(
                profile, port, cacheCertPassword, preLoadedCertificate, cancellationToken),
            _ => throw new NotSupportedException($"OAuth2 flow '{flow}' is not supported.")
        };
    }

    /// <summary>
    /// Refreshes a token for the specified profile.
    /// </summary>
    public async Task<AuthenticationResult> RefreshTokenAsync(
        AuthProfile profile,
        CancellationToken cancellationToken = default)
    {
        var app = await GetOrCreatePublicClientAsync(profile, cancellationToken);
        var accounts = await app.GetAccountsAsync();
        var account = accounts.FirstOrDefault();

        if (account == null)
        {
            throw new InvalidOperationException(
                "No cached account found. Please authenticate first using 'login' or 'get-token'.");
        }

        try
        {
            var result = await app.AcquireTokenSilent(profile.Scopes, account)
                .ExecuteAsync(cancellationToken);

            return ConvertResult(result);
        }
        catch (MsalUiRequiredException)
        {
            throw new InvalidOperationException(
                "Token cannot be refreshed silently. User interaction required. " +
                "Please re-authenticate using the appropriate flow.");
        }
    }

    private async Task<AuthenticationResult> AuthenticateClientCredentialsAsync(
        AuthProfile profile,
        bool cacheCertPassword,
        X509Certificate2? preLoadedCertificate,
        CancellationToken cancellationToken)
    {
        var app = await GetOrCreateConfidentialClientAsync(profile, cacheCertPassword, preLoadedCertificate, cancellationToken);

        var scopes = profile.Scopes.Any() 
            ? profile.Scopes.ToArray() 
            : new[] { $"{profile.Resource}/.default" };

        var result = await app.AcquireTokenForClient(scopes)
            .ExecuteAsync(cancellationToken);

        return ConvertResult(result);
    }

    private async Task<AuthenticationResult> AuthenticateAuthorizationCodeAsync(
        AuthProfile profile,
        int? port,
        bool cacheCertPassword,
        X509Certificate2? preLoadedCertificate,
        CancellationToken cancellationToken)
    {
        var app = await GetOrCreatePublicClientAsync(profile, cancellationToken);
        var redirectUri = GetRedirectUri(profile, port);

        // Try silent authentication first
        var accounts = await app.GetAccountsAsync();
        if (accounts.Any())
        {
            try
            {
                var result = await app.AcquireTokenSilent(profile.Scopes, accounts.First())
                    .ExecuteAsync(cancellationToken);
                return ConvertResult(result, isFromCache: true);
            }
            catch (MsalUiRequiredException)
            {
                // Fall through to interactive authentication
            }
        }

        // Interactive authentication with authorization code flow
        var interactiveResult = await app.AcquireTokenInteractive(profile.Scopes)
            .WithPrompt(Microsoft.Identity.Client.Prompt.SelectAccount)
            .ExecuteAsync(cancellationToken);

        return ConvertResult(interactiveResult);
    }

    private async Task<AuthenticationResult> AuthenticateDeviceCodeAsync(
        AuthProfile profile,
        bool cacheCertPassword,
        X509Certificate2? preLoadedCertificate,
        CancellationToken cancellationToken)
    {
        var app = await GetOrCreatePublicClientAsync(profile, cancellationToken);

        var result = await app.AcquireTokenWithDeviceCode(
            profile.Scopes,
            deviceCodeResult =>
            {
                AnsiConsole.MarkupLine($"\n[yellow]Device Code Authentication[/]");
                AnsiConsole.MarkupLine($"[cyan]{deviceCodeResult.Message}[/]\n");
                AnsiConsole.MarkupLine($"Code: [bold green]{deviceCodeResult.UserCode}[/]");
                AnsiConsole.MarkupLine($"URL: [link]{deviceCodeResult.VerificationUrl}[/]\n");

                return Task.CompletedTask;
            })
            .ExecuteAsync(cancellationToken);

        return ConvertResult(result);
    }

    private async Task<AuthenticationResult> AuthenticateInteractiveBrowserAsync(
        AuthProfile profile,
        int? port,
        bool cacheCertPassword,
        X509Certificate2? preLoadedCertificate,
        CancellationToken cancellationToken)
    {
        var app = await GetOrCreatePublicClientAsync(profile, cancellationToken);
        var redirectUri = GetRedirectUri(profile, port);

        // Try silent authentication first
        var accounts = await app.GetAccountsAsync();
        if (accounts.Any())
        {
            try
            {
                var result = await app.AcquireTokenSilent(profile.Scopes, accounts.First())
                    .ExecuteAsync(cancellationToken);
                return ConvertResult(result, isFromCache: true);
            }
            catch (MsalUiRequiredException)
            {
                // Fall through to interactive authentication
            }
        }

        var interactiveResult = await app.AcquireTokenInteractive(profile.Scopes)
            .WithUseEmbeddedWebView(false) // Use system browser
            .ExecuteAsync(cancellationToken);

        return ConvertResult(interactiveResult);
    }

    private async Task<IPublicClientApplication> GetOrCreatePublicClientAsync(
        AuthProfile profile,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"public:{profile.Name}";
        
        if (_clientCache.TryGetValue(cacheKey, out var cached) && cached is IPublicClientApplication publicApp)
        {
            return publicApp;
        }

        var authority = $"https://login.microsoftonline.com/{profile.TenantId}";
        var builder = PublicClientApplicationBuilder
            .Create(profile.ClientId)
            .WithAuthority(authority);

        if (!string.IsNullOrWhiteSpace(profile.RedirectUri))
        {
            builder.WithRedirectUri(profile.RedirectUri);
        }

        var app = builder.Build();

        // Setup token cache
        var tokenCache = new SecureTokenCache(_secureStorage, cacheKey);
        tokenCache.RegisterCache(app.UserTokenCache);

        _clientCache[cacheKey] = app;
        return app;
    }

    private async Task<IConfidentialClientApplication> GetOrCreateConfidentialClientAsync(
        AuthProfile profile,
        bool cacheCertPassword,
        X509Certificate2? preLoadedCertificate,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"confidential:{profile.Name}";
        
        if (_clientCache.TryGetValue(cacheKey, out var cached) && cached is IConfidentialClientApplication confidentialApp)
        {
            return confidentialApp;
        }

        var authority = $"https://login.microsoftonline.com/{profile.TenantId}";
        var builder = ConfidentialClientApplicationBuilder
            .Create(profile.ClientId)
            .WithAuthority(authority);

        // Configure authentication method
        switch (profile.AuthMethod)
        {
            case AuthenticationMethod.ClientSecret:
                var secret = await _configService.GetClientSecretAsync(profile.Name, cancellationToken);
                if (string.IsNullOrWhiteSpace(secret))
                {
                    throw new InvalidOperationException("Client secret not found for profile.");
                }
                builder.WithClientSecret(secret);
                break;

            case AuthenticationMethod.Certificate:
            case AuthenticationMethod.PasswordlessCertificate:
                // Use pre-loaded certificate if provided, otherwise load it now
                var certificate = preLoadedCertificate;
                if (certificate == null)
                {
                    var cachedPassword = cacheCertPassword
                        ? await _configService.GetCertificatePasswordAsync(profile.Name, cancellationToken)
                        : null;

                    certificate = CertificateLoader.LoadCertificate(
                        profile.CertificatePath!,
                        cachedPassword,
                        cacheCertPassword,
                        promptForPassword: false); // Don't prompt - should be pre-loaded
                }

                builder.WithCertificate(certificate);
                break;

            default:
                throw new NotSupportedException($"Authentication method '{profile.AuthMethod}' is not supported.");
        }

        var app = builder.Build();

        // Setup token cache
        var tokenCache = new SecureTokenCache(_secureStorage, cacheKey);
        tokenCache.RegisterCache(app.AppTokenCache);

        _clientCache[cacheKey] = app;
        return app;
    }

    private static string GetRedirectUri(AuthProfile profile, int? port)
    {
        if (!string.IsNullOrWhiteSpace(profile.RedirectUri))
        {
            return profile.RedirectUri;
        }

        // Smart port selection: 8080 -> 5000 -> random
        var selectedPort = port ?? 8080;
        return $"http://localhost:{selectedPort}";
    }

    private static AuthenticationResult ConvertResult(
        Microsoft.Identity.Client.AuthenticationResult msalResult,
        bool isFromCache = false)
    {
        return new AuthenticationResult
        {
            AccessToken = msalResult.AccessToken,
            ExpiresOn = msalResult.ExpiresOn,
            Scopes = msalResult.Scopes.ToArray(),
            TokenType = msalResult.TokenType,
            IsFromCache = isFromCache
        };
    }
}
