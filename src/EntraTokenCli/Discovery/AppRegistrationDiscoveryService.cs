using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Identity.Client;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Abstractions;
using EntraTokenCli.Storage;
using Spectre.Console;

namespace EntraTokenCli.Discovery;

/// <summary>
/// Service for discovering Azure AD app registrations with auto-consent.
/// </summary>
public class AppRegistrationDiscoveryService
{
    private readonly ISecureStorage _secureStorage;
    private GraphServiceClient? _graphClient;
    private const string GraphCacheKey = "graph-discovery";

    public AppRegistrationDiscoveryService(ISecureStorage secureStorage)
    {
        _secureStorage = secureStorage;
    }

    /// <summary>
    /// Searches for app registrations by display name with wildcard support.
    /// </summary>
    public async Task<List<Application>> SearchApplicationsAsync(
        string tenantId,
        string searchPattern,
        CancellationToken cancellationToken = default)
    {
        var graphClient = await GetGraphClientAsync(tenantId, cancellationToken);

        try
        {
            var applications = new List<Application>();

            // Convert wildcard pattern to OData filter
            string? filter = null;
            if (!string.IsNullOrWhiteSpace(searchPattern) && searchPattern != "*")
            {
                // Simple wildcard conversion (startsWith for prefix, contains for middle)
                if (searchPattern.EndsWith('*') && searchPattern.StartsWith('*'))
                {
                    var term = searchPattern.Trim('*');
                    filter = $"startswith(displayName,'{term}')";
                }
                else if (searchPattern.EndsWith('*'))
                {
                    var term = searchPattern.TrimEnd('*');
                    filter = $"startswith(displayName,'{term}')";
                }
                else if (searchPattern.StartsWith('*'))
                {
                    // Contains not directly supported, fetch all and filter client-side
                    filter = null;
                }
                else
                {
                    filter = $"displayName eq '{searchPattern}'";
                }
            }

            var result = await graphClient.Applications.GetAsync(config =>
            {
                config.QueryParameters.Select = new[] { "id", "appId", "displayName", "createdDateTime", "publisherDomain" };
                config.QueryParameters.Top = 999;
                if (filter != null)
                {
                    config.QueryParameters.Filter = filter;
                }
            }, cancellationToken);

            if (result?.Value != null)
            {
                applications.AddRange(result.Value);

                // Client-side wildcard filtering if needed
                if (!string.IsNullOrWhiteSpace(searchPattern) && 
                    searchPattern.Contains('*', StringComparison.Ordinal))
                {
                    var regex = new System.Text.RegularExpressions.Regex(
                        "^" + System.Text.RegularExpressions.Regex.Escape(searchPattern)
                            .Replace("\\*", ".*") + "$",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                    applications = applications
                        .Where(a => a.DisplayName != null && regex.IsMatch(a.DisplayName))
                        .ToList();
                }
            }

            return applications;
        }
        catch (ServiceException ex) when (ex.ResponseStatusCode == 403 || ex.ResponseStatusCode == 401)
        {
            // Missing permissions - trigger consent flow
            AnsiConsole.MarkupLine("[yellow]⚠ Missing required permissions to access app registrations.[/]");
            AnsiConsole.MarkupLine("[cyan]The following permissions are required:[/]");
            AnsiConsole.MarkupLine("  • Application.Read.All");
            AnsiConsole.MarkupLine("  • Directory.Read.All");
            AnsiConsole.WriteLine();

            var shouldConsent = AnsiConsole.Confirm(
                "Would you like to request these permissions now?", 
                defaultValue: true);

            if (shouldConsent)
            {
                await RequestConsentAsync(tenantId, cancellationToken);
                
                // Retry after consent
                return await SearchApplicationsAsync(tenantId, searchPattern, cancellationToken);
            }

            throw new InvalidOperationException(
                "Cannot access app registrations without required permissions.");
        }
    }

    /// <summary>
    /// Gets a Graph client with appropriate permissions.
    /// </summary>
    private async Task<GraphServiceClient> GetGraphClientAsync(
        string tenantId,
        CancellationToken cancellationToken)
    {
        if (_graphClient != null)
        {
            return _graphClient;
        }

        var scopes = new[] { "Application.Read.All", "Directory.Read.All" };

        // Use Microsoft Graph PowerShell client ID (public client)
        // This is a well-known client ID that has permissions for Graph API
        var clientId = "14d82eec-204b-4c2f-b7e8-296a70dab67e";
        var authority = $"https://login.microsoftonline.com/{tenantId}";

        var app = PublicClientApplicationBuilder
            .Create(clientId)
            .WithAuthority(authority)
            .WithRedirectUri("http://localhost")
            .Build();

        // Setup token cache
        var tokenCache = new Authentication.SecureTokenCache(_secureStorage, GraphCacheKey);
        tokenCache.RegisterCache(app.UserTokenCache);

        // Try silent authentication first
        var accounts = await app.GetAccountsAsync();
        AuthenticationResult? authResult = null;

        if (accounts.Any())
        {
            try
            {
                authResult = await app.AcquireTokenSilent(scopes, accounts.First())
                    .ExecuteAsync(cancellationToken);
            }
            catch (MsalUiRequiredException)
            {
                // Fall through to interactive auth
            }
        }

        // Interactive authentication if needed
        authResult ??= await app.AcquireTokenInteractive(scopes)
            .WithPrompt(Microsoft.Identity.Client.Prompt.SelectAccount)
            .ExecuteAsync(cancellationToken);

        _graphClient = new GraphServiceClient(
            new DelegateAuthProvider(authResult.AccessToken)
        );

        return _graphClient;
    }

    /// <summary>
    /// Requests admin consent for required permissions.
    /// </summary>
    private async Task RequestConsentAsync(string tenantId, CancellationToken cancellationToken)
    {
        var scopes = new[] { "Application.Read.All", "Directory.Read.All" };
        var clientId = "14d82eec-204b-4c2f-b7e8-296a70dab67e";
        var authority = $"https://login.microsoftonline.com/{tenantId}";

        var app = PublicClientApplicationBuilder
            .Create(clientId)
            .WithAuthority(authority)
            .WithRedirectUri("http://localhost")
            .Build();

        var tokenCache = new Authentication.SecureTokenCache(_secureStorage, GraphCacheKey);
        tokenCache.RegisterCache(app.UserTokenCache);

        AnsiConsole.MarkupLine("[yellow]Opening browser for admin consent...[/]");

        // Use device code flow for better cross-platform support
        await app.AcquireTokenWithDeviceCode(
            scopes,
            deviceCodeResult =>
            {
                AnsiConsole.MarkupLine($"\n[cyan]{deviceCodeResult.Message}[/]\n");
                AnsiConsole.MarkupLine($"Code: [bold green]{deviceCodeResult.UserCode}[/]");
                AnsiConsole.MarkupLine($"URL: [link]{deviceCodeResult.VerificationUrl}[/]\n");
                return Task.CompletedTask;
            })
            .ExecuteAsync(cancellationToken);

        AnsiConsole.MarkupLine("[green]✓ Consent granted successfully![/]");
    }
}

/// <summary>
/// Simple authentication provider for Microsoft Graph SDK.
/// </summary>
internal class DelegateAuthProvider : IAuthenticationProvider
{
    private readonly string _accessToken;

    public DelegateAuthProvider(string accessToken)
    {
        _accessToken = accessToken;
    }

    public Task AuthenticateRequestAsync(RequestInformation request, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
    {
        request.Headers.Add("Authorization", $"Bearer {_accessToken}");
        return Task.CompletedTask;
    }
}
