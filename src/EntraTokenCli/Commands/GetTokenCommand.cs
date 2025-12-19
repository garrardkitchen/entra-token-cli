using Spectre.Console.Cli;
using System.ComponentModel;
using System.Security.Cryptography.X509Certificates;
using EntraTokenCli.Configuration;
using EntraTokenCli.Authentication;
using EntraTokenCli.UI;

namespace EntraTokenCli.Commands;

public class GetTokenSettings : CommandSettings
{
    [CommandOption("-p|--profile <PROFILE>")]
    [Description("Profile name to use for authentication")]
    public string? ProfileName { get; init; }

    [CommandOption("-f|--flow <FLOW>")]
    [Description("OAuth2 flow to use (AuthorizationCode, ClientCredentials, DeviceCode, InteractiveBrowser)")]
    public string? Flow { get; init; }

    [CommandOption("--no-clipboard")]
    [Description("Do not copy token to clipboard")]
    public bool NoClipboard { get; init; }

    [CommandOption("--redirect-uri <URI>")]
    [Description("Custom redirect URI (overrides profile setting)")]
    public string? RedirectUri { get; init; }

    [CommandOption("--port <PORT>")]
    [Description("Port for localhost redirect (default: 8080, fallback: 5000, then random)")]
    public int? Port { get; init; }

    [CommandOption("--cache-cert-password")]
    [Description("Use cached certificate password if available")]
    public bool CacheCertPassword { get; init; }

    [CommandOption("--warn-expiry <MINUTES>")]
    [Description("Warn if cached token expires within specified minutes (default: 5)")]
    public int WarnExpiryMinutes { get; init; } = 5;

    [CommandOption("-s|--scope <SCOPE>")]
    [Description("Override scope(s) for this request (comma-separated). Examples: 'https://graph.microsoft.com/.default', 'api://myapi/access'")]
    public string? Scope { get; init; }
}

public class GetTokenCommand : AsyncCommand<GetTokenSettings>
{
    private readonly ConfigService _configService;
    private readonly MsalAuthService _authService;

    public GetTokenCommand(ConfigService configService, MsalAuthService authService)
    {
        _configService = configService;
        _authService = authService;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, GetTokenSettings settings)
    {
        try
        {
            await _configService.LoadProfilesAsync();

            // Select profile
            AuthProfile profile;
            if (!string.IsNullOrWhiteSpace(settings.ProfileName))
            {
                var foundProfile = _configService.GetProfile(settings.ProfileName);
                if (foundProfile == null)
                {
                    ConsoleUi.DisplayError($"Profile '{settings.ProfileName}' not found.");
                    return 1;
                }
                profile = foundProfile;
            }
            else
            {
                profile = ConsoleUi.PromptForProfile(_configService.GetProfiles());
            }

            // Apply redirect URI override
            if (!string.IsNullOrWhiteSpace(settings.RedirectUri))
            {
                profile = profile with { RedirectUri = settings.RedirectUri };
            }

            // Apply scope override
            if (!string.IsNullOrWhiteSpace(settings.Scope))
            {
                var overrideScopes = settings.Scope.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .ToList();
                profile = profile with { Scopes = overrideScopes };
            }

            // Determine flow
            OAuth2Flow flow;
            if (!string.IsNullOrWhiteSpace(settings.Flow))
            {
                // User explicitly specified flow
                if (!Enum.TryParse<OAuth2Flow>(settings.Flow, ignoreCase: true, out var parsedFlow))
                {
                    ConsoleUi.DisplayError($"Invalid flow: {settings.Flow}");
                    return 1;
                }
                flow = parsedFlow;
            }
            else if (profile.DefaultFlow.HasValue)
            {
                // Use profile's default flow
                flow = profile.DefaultFlow.Value;
            }
            else
            {
                // Infer flow from authentication method
                flow = profile.AuthMethod switch
                {
                    AuthenticationMethod.ClientSecret => OAuth2Flow.ClientCredentials,
                    AuthenticationMethod.Certificate => OAuth2Flow.ClientCredentials,
                    _ => OAuth2Flow.InteractiveBrowser
                };
            }

            // Pre-load certificate if needed (to avoid password prompt during spinner)
            X509Certificate2? preLoadedCertificate = null;
            if (profile.AuthMethod == AuthenticationMethod.Certificate || 
                profile.AuthMethod == AuthenticationMethod.PasswordlessCertificate)
            {
                preLoadedCertificate = await PreLoadCertificateAsync(profile, settings.CacheCertPassword);
            }

            // Authenticate
            var result = await ConsoleUi.ShowSpinnerAsync(
                $"Authenticating using {flow} flow...",
                async () => await _authService.AuthenticateAsync(
                    profile,
                    flow,
                    settings.Port,
                    settings.CacheCertPassword,
                    preLoadedCertificate));

            // Check expiry warning
            if (result.IsFromCache)
            {
                var timeUntilExpiry = result.ExpiresOn - DateTimeOffset.UtcNow;
                if (timeUntilExpiry.TotalMinutes <= settings.WarnExpiryMinutes)
                {
                    ConsoleUi.DisplayWarning(
                        $"Token expires in {timeUntilExpiry.TotalMinutes:F1} minutes. " +
                        $"Consider refreshing using 'entratool refresh -p {profile.Name}'");
                }
            }

            // Display token
            await ConsoleUi.DisplayTokenAsync(
                result,
                !settings.NoClipboard,
                _configService.GetLastTokenFilePath());

            return 0;
        }
        catch (Exception ex)
        {
            ConsoleUi.DisplayError(ex.Message);
            return 1;
        }
    }

    /// <summary>
    /// Pre-loads certificate to prompt for password before showing spinner.
    /// This avoids concurrent interactive operations (prompt + spinner).
    /// </summary>
    private async Task<X509Certificate2> PreLoadCertificateAsync(AuthProfile profile, bool cacheCertPassword)
    {
        var cachedPassword = cacheCertPassword
            ? await _configService.GetCertificatePasswordAsync(profile.Name)
            : null;

        // This will prompt for password if needed, BEFORE we start the spinner
        return Authentication.CertificateLoader.LoadCertificate(
            profile.CertificatePath!,
            cachedPassword,
            cacheCertPassword,
            promptForPassword: true);
    }
}
