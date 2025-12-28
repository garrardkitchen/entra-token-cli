using System.ComponentModel;
using EntraAuthCli.Authentication;
using EntraAuthCli.Configuration;
using EntraAuthCli.UI;
using Spectre.Console.Cli;

namespace EntraAuthCli.Commands;

public class RefreshSettings : CommandSettings
{
    [CommandOption("-p|--profile <PROFILE>")]
    [Description("Profile name to refresh token for")]
    public string? ProfileName { get; init; }
}

public class RefreshCommand : AsyncCommand<RefreshSettings>
{
    private readonly ConfigService _configService;
    private readonly MsalAuthService _authService;

    public RefreshCommand(ConfigService configService, MsalAuthService authService)
    {
        _configService = configService;
        _authService = authService;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, RefreshSettings settings)
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

            // Refresh token
            var result = await ConsoleUi.ShowSpinnerAsync(
                "Refreshing token...",
                async () => await _authService.RefreshTokenAsync(profile));

            ConsoleUi.DisplaySuccess("Token refreshed successfully!");

            // Display token
            await ConsoleUi.DisplayTokenAsync(
                result,
                copyToClipboard: true,
                _configService.GetLastTokenFilePath());

            return 0;
        }
        catch (Exception ex)
        {
            ConsoleUi.DisplayError(ex.Message);
            return 1;
        }
    }
}
