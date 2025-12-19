using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using EntraTokenCli.Configuration;
using EntraTokenCli.UI;

namespace EntraTokenCli.Commands;

public class ConfigSettings : CommandSettings
{
}

[Description("Manage authentication profiles")]
public class ConfigCommand : Command<ConfigSettings>
{
    public override int Execute(CommandContext context, ConfigSettings settings)
    {
        AnsiConsole.MarkupLine("[yellow]Use one of the following subcommands:[/]");
        AnsiConsole.MarkupLine("  [cyan]entratool config create[/]  - Create a new profile");
        AnsiConsole.MarkupLine("  [cyan]entratool config list[/]    - List all profiles");
        AnsiConsole.MarkupLine("  [cyan]entratool config edit[/]    - Edit a profile");
        AnsiConsole.MarkupLine("  [cyan]entratool config delete[/]  - Delete a profile");
        AnsiConsole.MarkupLine("  [cyan]entratool config export[/]  - Export a profile");
        AnsiConsole.MarkupLine("  [cyan]entratool config import[/]  - Import a profile");
        return 0;
    }
}

public class ConfigListSettings : CommandSettings
{
}

[Description("List all authentication profiles")]
public class ConfigListCommand : AsyncCommand<ConfigListSettings>
{
    private readonly ConfigService _configService;

    public ConfigListCommand(ConfigService configService)
    {
        _configService = configService;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ConfigListSettings settings)
    {
        try
        {
            await _configService.LoadProfilesAsync();
            var profiles = _configService.GetProfiles();

            if (profiles.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No profiles found.[/]");
                return 0;
            }

            var table = new Table()
                .BorderColor(Color.Grey)
                .AddColumn("[bold]Name[/]")
                .AddColumn("[bold]Tenant ID[/]")
                .AddColumn("[bold]Client ID[/]")
                .AddColumn("[bold]Auth Method[/]")
                .AddColumn("[bold]Default Flow[/]")
                .AddColumn("[bold]Updated[/]");

            foreach (var profile in profiles.OrderBy(p => p.Name))
            {
                table.AddRow(
                    profile.Name,
                    profile.TenantId,
                    profile.ClientId,
                    profile.AuthMethod.ToString(),
                    profile.DefaultFlow?.ToString() ?? "N/A",
                    profile.UpdatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm")
                );
            }

            AnsiConsole.Write(table);
            return 0;
        }
        catch (Exception ex)
        {
            ConsoleUi.DisplayError(ex.Message);
            return 1;
        }
    }
}

public class ConfigCreateSettings : CommandSettings
{
}

[Description("Create a new authentication profile")]
public class ConfigCreateCommand : AsyncCommand<ConfigCreateSettings>
{
    private readonly ConfigService _configService;

    public ConfigCreateCommand(ConfigService configService)
    {
        _configService = configService;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ConfigCreateSettings settings)
    {
        try
        {
            await _configService.LoadProfilesAsync();

            AnsiConsole.MarkupLine("[yellow]Create New Authentication Profile[/]\n");

            var profile = new AuthProfile
            {
                Name = AnsiConsole.Ask<string>("Profile [cyan]name[/]:"),
                TenantId = AnsiConsole.Ask<string>("Tenant [cyan]ID[/]:"),
                ClientId = AnsiConsole.Ask<string>("Client [cyan]ID[/]:"),
            };

            // Check if profile already exists
            if (_configService.GetProfile(profile.Name) != null)
            {
                ConsoleUi.DisplayError($"Profile '{profile.Name}' already exists.");
                return 1;
            }

            // Scopes
            AnsiConsole.MarkupLine("[dim]Examples:[/]");
            AnsiConsole.MarkupLine("[dim]  - Microsoft Graph API: https://graph.microsoft.com/.default[/]");
            AnsiConsole.MarkupLine("[dim]  - Azure Management API: https://management.azure.com/.default[/]");
            AnsiConsole.MarkupLine("[dim]  - Custom API: api://YOUR-API-CLIENT-ID/.default[/]");
            AnsiConsole.MarkupLine("[dim]  - Specific scope: api://YOUR-API-CLIENT-ID/access[/]\n");
            var scopesInput = AnsiConsole.Ask<string>("Scopes (comma-separated):", "https://graph.microsoft.com/.default");
            profile.Scopes = scopesInput.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToList();

            // Auth method
            profile.AuthMethod = AnsiConsole.Prompt(
                new SelectionPrompt<AuthenticationMethod>()
                    .Title("Authentication [cyan]method[/]:")
                    .AddChoices(Enum.GetValues<AuthenticationMethod>()));

            // Handle authentication method specific settings
            switch (profile.AuthMethod)
            {
                case AuthenticationMethod.ClientSecret:
                    var secret = AnsiConsole.Prompt(
                        new TextPrompt<string>("Client [cyan]secret[/]:")
                            .PromptStyle("yellow")
                            .Secret());
                    await _configService.StoreClientSecretAsync(profile.Name, secret);
                    break;

                case AuthenticationMethod.Certificate:
                case AuthenticationMethod.PasswordlessCertificate:
                    profile.CertificatePath = AnsiConsole.Ask<string>("Certificate [cyan]path[/] (.pfx):");
                    
                    if (profile.AuthMethod == AuthenticationMethod.Certificate)
                    {
                        profile.CacheCertificatePassword = AnsiConsole.Confirm(
                            "Cache certificate password securely?", defaultValue: false);

                        if (profile.CacheCertificatePassword)
                        {
                            var certPassword = AnsiConsole.Prompt(
                                new TextPrompt<string>("Certificate [cyan]password[/]:")
                                    .PromptStyle("yellow")
                                    .Secret()
                                    .AllowEmpty());

                            if (!string.IsNullOrWhiteSpace(certPassword))
                            {
                                await _configService.StoreCertificatePasswordAsync(profile.Name, certPassword);
                            }
                        }
                    }
                    break;
            }

            // Default flow
            var configureFlow = AnsiConsole.Confirm("Set default OAuth2 flow?", defaultValue: false);
            if (configureFlow)
            {
                profile.DefaultFlow = AnsiConsole.Prompt(
                    new SelectionPrompt<OAuth2Flow>()
                        .Title("Default [cyan]OAuth2 flow[/]:")
                        .AddChoices(Enum.GetValues<OAuth2Flow>()));
            }

            // Redirect URI
            var configureRedirect = AnsiConsole.Confirm("Configure custom redirect URI?", defaultValue: false);
            if (configureRedirect)
            {
                profile.RedirectUri = AnsiConsole.Ask<string>("Redirect [cyan]URI[/]:");
            }

            await _configService.SaveProfileAsync(profile);

            ConsoleUi.DisplaySuccess($"Profile '{profile.Name}' created successfully!");

            return 0;
        }
        catch (Exception ex)
        {
            ConsoleUi.DisplayError(ex.Message);
            return 1;
        }
    }
}

public class ConfigEditSettings : CommandSettings
{
    [CommandOption("-p|--profile <PROFILE>")]
    [Description("Profile name to edit")]
    public string? ProfileName { get; init; }
}

[Description("Edit an existing authentication profile")]
public class ConfigEditCommand : AsyncCommand<ConfigEditSettings>
{
    private readonly ConfigService _configService;

    public ConfigEditCommand(ConfigService configService)
    {
        _configService = configService;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ConfigEditSettings settings)
    {
        try
        {
            await _configService.LoadProfilesAsync();

            // Select profile to edit
            string profileName;
            if (!string.IsNullOrWhiteSpace(settings.ProfileName))
            {
                profileName = settings.ProfileName;
            }
            else
            {
                var profile = ConsoleUi.PromptForProfile(_configService.GetProfiles());
                profileName = profile.Name;
            }

            var existingProfile = _configService.GetProfile(profileName);
            if (existingProfile == null)
            {
                ConsoleUi.DisplayError($"Profile '{profileName}' not found.");
                return 1;
            }

            AnsiConsole.MarkupLine($"[yellow]Editing Profile: {profileName}[/]\n");
            AnsiConsole.MarkupLine("[dim]Press Enter to keep current value[/]\n");

            // Create updated profile
            var updatedProfile = existingProfile with
            {
                TenantId = AnsiConsole.Ask("Tenant [cyan]ID[/]:", existingProfile.TenantId),
                ClientId = AnsiConsole.Ask("Client [cyan]ID[/]:", existingProfile.ClientId),
                UpdatedAt = DateTimeOffset.UtcNow
            };

            // Update scopes
            var currentScopes = string.Join(", ", existingProfile.Scopes);
            AnsiConsole.MarkupLine("[dim]Examples:[/]");
            AnsiConsole.MarkupLine("[dim]  - Microsoft Graph API: https://graph.microsoft.com/.default[/]");
            AnsiConsole.MarkupLine("[dim]  - Azure Management API: https://management.azure.com/.default[/]");
            AnsiConsole.MarkupLine("[dim]  - Custom API: api://YOUR-API-CLIENT-ID/.default[/]");
            AnsiConsole.MarkupLine("[dim]  - Specific scope: api://YOUR-API-CLIENT-ID/access[/]\n");
            var scopesInput = AnsiConsole.Ask("Scopes (comma-separated):", currentScopes);
            updatedProfile.Scopes = scopesInput.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToList();

            // Update auth method
            var changeAuthMethod = AnsiConsole.Confirm(
                $"Change authentication method? (current: {existingProfile.AuthMethod})", 
                defaultValue: false);

            if (changeAuthMethod)
            {
                updatedProfile.AuthMethod = AnsiConsole.Prompt(
                    new SelectionPrompt<AuthenticationMethod>()
                        .Title("Authentication [cyan]method[/]:")
                        .AddChoices(Enum.GetValues<AuthenticationMethod>()));

                // Handle authentication method specific settings
                switch (updatedProfile.AuthMethod)
                {
                    case AuthenticationMethod.ClientSecret:
                        var updateSecret = AnsiConsole.Confirm("Update client secret?", defaultValue: true);
                        if (updateSecret)
                        {
                            var secret = AnsiConsole.Prompt(
                                new TextPrompt<string>("Client [cyan]secret[/]:")
                                    .PromptStyle("yellow")
                                    .Secret());
                            await _configService.StoreClientSecretAsync(profileName, secret);
                        }
                        break;

                    case AuthenticationMethod.Certificate:
                    case AuthenticationMethod.PasswordlessCertificate:
                        updatedProfile.CertificatePath = AnsiConsole.Ask(
                            "Certificate [cyan]path[/] (.pfx):", 
                            existingProfile.CertificatePath ?? string.Empty);
                        
                        if (updatedProfile.AuthMethod == AuthenticationMethod.Certificate)
                        {
                            updatedProfile.CacheCertificatePassword = AnsiConsole.Confirm(
                                "Cache certificate password securely?", 
                                defaultValue: existingProfile.CacheCertificatePassword);

                            if (updatedProfile.CacheCertificatePassword)
                            {
                                var updateCertPassword = AnsiConsole.Confirm(
                                    "Update certificate password?", 
                                    defaultValue: false);

                                if (updateCertPassword)
                                {
                                    var certPassword = AnsiConsole.Prompt(
                                        new TextPrompt<string>("Certificate [cyan]password[/]:")
                                            .PromptStyle("yellow")
                                            .Secret()
                                            .AllowEmpty());

                                    if (!string.IsNullOrWhiteSpace(certPassword))
                                    {
                                        await _configService.StoreCertificatePasswordAsync(profileName, certPassword);
                                    }
                                }
                            }
                        }
                        break;
                }
            }

            // Update default flow
            var changeFlow = AnsiConsole.Confirm(
                $"Change default OAuth2 flow? (current: {existingProfile.DefaultFlow?.ToString() ?? "None"})", 
                defaultValue: false);

            if (changeFlow)
            {
                var setFlow = AnsiConsole.Confirm("Set default OAuth2 flow?", defaultValue: existingProfile.DefaultFlow.HasValue);
                if (setFlow)
                {
                    updatedProfile.DefaultFlow = AnsiConsole.Prompt(
                        new SelectionPrompt<OAuth2Flow>()
                            .Title("Default [cyan]OAuth2 flow[/]:")
                            .AddChoices(Enum.GetValues<OAuth2Flow>()));
                }
                else
                {
                    updatedProfile.DefaultFlow = null;
                }
            }

            // Update redirect URI
            var changeRedirect = AnsiConsole.Confirm(
                $"Change redirect URI? (current: {existingProfile.RedirectUri ?? "None"})", 
                defaultValue: false);

            if (changeRedirect)
            {
                var setRedirect = AnsiConsole.Confirm("Set custom redirect URI?", defaultValue: !string.IsNullOrWhiteSpace(existingProfile.RedirectUri));
                if (setRedirect)
                {
                    updatedProfile.RedirectUri = AnsiConsole.Ask(
                        "Redirect [cyan]URI[/]:", 
                        existingProfile.RedirectUri ?? "http://localhost:8080");
                }
                else
                {
                    updatedProfile.RedirectUri = null;
                }
            }

            await _configService.SaveProfileAsync(updatedProfile);

            ConsoleUi.DisplaySuccess($"Profile '{profileName}' updated successfully!");

            return 0;
        }
        catch (Exception ex)
        {
            ConsoleUi.DisplayError(ex.Message);
            return 1;
        }
    }
}

public class ConfigDeleteSettings : CommandSettings
{
    [CommandOption("-p|--profile <PROFILE>")]
    [Description("Profile name to delete")]
    public string? ProfileName { get; init; }
}

[Description("Delete an authentication profile")]
public class ConfigDeleteCommand : AsyncCommand<ConfigDeleteSettings>
{
    private readonly ConfigService _configService;

    public ConfigDeleteCommand(ConfigService configService)
    {
        _configService = configService;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ConfigDeleteSettings settings)
    {
        try
        {
            await _configService.LoadProfilesAsync();

            string profileName;
            if (!string.IsNullOrWhiteSpace(settings.ProfileName))
            {
                profileName = settings.ProfileName;
            }
            else
            {
                var profile = ConsoleUi.PromptForProfile(_configService.GetProfiles());
                profileName = profile.Name;
            }

            var confirm = AnsiConsole.Confirm($"Are you sure you want to delete profile '{profileName}'?");
            if (!confirm)
            {
                AnsiConsole.MarkupLine("[yellow]Operation cancelled.[/]");
                return 0;
            }

            await _configService.DeleteProfileAsync(profileName);
            ConsoleUi.DisplaySuccess($"Profile '{profileName}' deleted successfully!");

            return 0;
        }
        catch (Exception ex)
        {
            ConsoleUi.DisplayError(ex.Message);
            return 1;
        }
    }
}

public class ConfigExportSettings : CommandSettings
{
    [CommandOption("-p|--profile <PROFILE>")]
    [Description("Profile name to export")]
    public string? ProfileName { get; init; }

    [CommandOption("--include-secrets")]
    [Description("Include secrets in export")]
    public bool IncludeSecrets { get; init; }

    [CommandOption("-o|--output <FILE>")]
    [Description("Output file path")]
    public string? OutputFile { get; init; }
}

[Description("Export an authentication profile")]
public class ConfigExportCommand : AsyncCommand<ConfigExportSettings>
{
    private readonly ConfigService _configService;
    private readonly ProfileExportService _exportService;

    public ConfigExportCommand(ConfigService configService, ProfileExportService exportService)
    {
        _configService = configService;
        _exportService = exportService;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ConfigExportSettings settings)
    {
        try
        {
            await _configService.LoadProfilesAsync();

            string profileName;
            if (!string.IsNullOrWhiteSpace(settings.ProfileName))
            {
                profileName = settings.ProfileName;
            }
            else
            {
                var profile = ConsoleUi.PromptForProfile(_configService.GetProfiles());
                profileName = profile.Name;
            }

            var passphrase = AnsiConsole.Prompt(
                new TextPrompt<string>("Encryption [cyan]passphrase[/]:")
                    .PromptStyle("yellow")
                    .Secret());

            var exportedData = await _exportService.ExportProfileAsync(
                profileName,
                passphrase,
                settings.IncludeSecrets);

            if (!string.IsNullOrWhiteSpace(settings.OutputFile))
            {
                await File.WriteAllTextAsync(settings.OutputFile, exportedData);
                ConsoleUi.DisplaySuccess($"Profile exported to: {settings.OutputFile}");
            }
            else
            {
                AnsiConsole.MarkupLine("\n[yellow]Exported Profile Data:[/]");
                AnsiConsole.WriteLine(exportedData);
                AnsiConsole.WriteLine();
            }

            return 0;
        }
        catch (Exception ex)
        {
            ConsoleUi.DisplayError(ex.Message);
            return 1;
        }
    }
}

public class ConfigImportSettings : CommandSettings
{
    [CommandOption("-i|--input <FILE>")]
    [Description("Input file path")]
    public string? InputFile { get; init; }

    [CommandOption("-n|--name <NAME>")]
    [Description("New profile name (optional)")]
    public string? NewName { get; init; }
}

[Description("Import an authentication profile")]
public class ConfigImportCommand : AsyncCommand<ConfigImportSettings>
{
    private readonly ConfigService _configService;
    private readonly ProfileExportService _exportService;

    public ConfigImportCommand(ConfigService configService, ProfileExportService exportService)
    {
        _configService = configService;
        _exportService = exportService;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ConfigImportSettings settings)
    {
        try
        {
            await _configService.LoadProfilesAsync();

            string encryptedData;
            if (!string.IsNullOrWhiteSpace(settings.InputFile))
            {
                encryptedData = await File.ReadAllTextAsync(settings.InputFile);
            }
            else
            {
                AnsiConsole.MarkupLine("[yellow]Paste encrypted profile data:[/]");
                encryptedData = Console.ReadLine() ?? string.Empty;
            }

            var passphrase = AnsiConsole.Prompt(
                new TextPrompt<string>("Decryption [cyan]passphrase[/]:")
                    .PromptStyle("yellow")
                    .Secret());

            var profile = await _exportService.ImportProfileAsync(
                encryptedData,
                passphrase,
                settings.NewName);

            ConsoleUi.DisplaySuccess($"Profile '{profile.Name}' imported successfully!");

            return 0;
        }
        catch (Exception ex)
        {
            ConsoleUi.DisplayError(ex.Message);
            return 1;
        }
    }
}
