using System.ComponentModel;
using EntraAuthCli.Configuration;
using EntraAuthCli.Discovery;
using Spectre.Console;
using Spectre.Console.Cli;

namespace EntraAuthCli.Commands;

public class DiscoverSettings : CommandSettings
{
    [CommandOption("-t|--tenant <TENANT>")]
    [Description("Tenant ID to search in")]
    public string? TenantId { get; init; }

    [CommandOption("-s|--search <PATTERN>")]
    [Description("Search pattern with wildcard support (e.g., 'MyApp*', '*Test*')")]
    public string SearchPattern { get; init; } = "*";
}

[Description("Discover Azure AD app registrations")]
public class DiscoverCommand : AsyncCommand<DiscoverSettings>
{
    private readonly AppRegistrationDiscoveryService _discoveryService;
    private readonly ConfigService _configService;

    public DiscoverCommand(
        AppRegistrationDiscoveryService discoveryService,
        ConfigService configService)
    {
        _discoveryService = discoveryService;
        _configService = configService;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, DiscoverSettings settings)
    {
        try
        {
            string tenantId;

            if (!string.IsNullOrWhiteSpace(settings.TenantId))
            {
                tenantId = settings.TenantId;
            }
            else
            {
                await _configService.LoadProfilesAsync();
                var profiles = _configService.GetProfiles();

                if (profiles.Count == 0)
                {
                    tenantId = AnsiConsole.Ask<string>("Enter [cyan]Tenant ID[/]:");
                }
                else
                {
                    // Get tenant from existing profile or prompt
                    var choices = profiles.Select(p => p.TenantId).Distinct().ToList();
                    choices.Add("(Enter manually)");

                    var selection = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("Select [cyan]tenant[/]:")
                            .AddChoices(choices));

                    if (selection == "(Enter manually)")
                    {
                        tenantId = AnsiConsole.Ask<string>("Enter [cyan]Tenant ID[/]:");
                    }
                    else
                    {
                        tenantId = selection;
                    }
                }
            }

            // Search for applications
            var applications = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Style.Parse("green bold"))
                .StartAsync(
                    $"Searching for applications matching '{settings.SearchPattern}'...",
                    async ctx => await _discoveryService.SearchApplicationsAsync(
                        tenantId,
                        settings.SearchPattern));

            if (applications.Count == 0)
            {
                AnsiConsole.MarkupLine($"[yellow]No applications found matching '{settings.SearchPattern}'[/]");
                return 0;
            }

            // Display results in table
            var table = new Table()
                .BorderColor(Color.Blue)
                .Border(TableBorder.Rounded)
                .AddColumn(new TableColumn("[bold cyan]Display Name[/]").Centered())
                .AddColumn(new TableColumn("[bold green]Client ID[/]").Centered())
                .AddColumn(new TableColumn("[bold yellow]Publisher Domain[/]").Centered())
                .AddColumn(new TableColumn("[bold grey]Created[/]").Centered());

            foreach (var app in applications.OrderBy(a => a.DisplayName))
            {
                table.AddRow(
                    $"[cyan]{app.DisplayName ?? "N/A"}[/]",
                    $"[green]{app.AppId ?? "N/A"}[/]",
                    $"[yellow]{app.PublisherDomain ?? "N/A"}[/]",
                    $"[grey]{app.CreatedDateTime?.ToLocalTime().ToString("yyyy-MM-dd") ?? "N/A"}[/]"
                );
            }

            AnsiConsole.Write(new Panel(table)
            {
                Header = new PanelHeader($"[bold yellow]🔍 Found {applications.Count} application(s)[/]"),
                Border = BoxBorder.Double,
                BorderStyle = new Style(Color.Blue)
            });

            // Offer to create profile from selection
            if (AnsiConsole.Confirm("[bold cyan]Would you like to create a profile from one of these?[/]", defaultValue: false))
            {
                var selectedApp = AnsiConsole.Prompt(
                    new SelectionPrompt<Microsoft.Graph.Models.Application>()
                        .Title("Select an [bold cyan]application[/]:")
                        .PageSize(10)
                        .MoreChoicesText("[grey](Move up and down to reveal more apps, or start typing to search)[/]")
                        .AddChoices(applications)
                        .UseConverter(a => $"[cyan]{a.DisplayName}[/] [grey]({a.AppId})[/]")
                        .HighlightStyle(new Style(Color.Cyan1, decoration: Decoration.Bold))
                        .EnableSearch());

                var profileName = AnsiConsole.Ask<string>(
                    "Profile [cyan]name[/]:",
                    defaultValue: selectedApp.DisplayName?.Replace(" ", "") ?? "profile");

                var profile = new AuthProfile
                {
                    Name = profileName,
                    TenantId = tenantId,
                    ClientId = selectedApp.AppId!,
                    Scopes = new List<string> { "https://graph.microsoft.com/.default" }
                };

                // Configure auth method
                profile.AuthMethod = AnsiConsole.Prompt(
                    new SelectionPrompt<AuthenticationMethod>()
                        .Title("Authentication [cyan]method[/]:")
                        .AddChoices(Enum.GetValues<AuthenticationMethod>()));

                // Handle secret/certificate
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
                                "Cache certificate password?", defaultValue: false);

                            if (profile.CacheCertificatePassword)
                            {
                                var certPassword = AnsiConsole.Prompt(
                                    new TextPrompt<string>("Certificate [cyan]password[/]:")
                                        .PromptStyle("yellow")
                                        .Secret()
                                        .AllowEmpty());

                                if (!string.IsNullOrWhiteSpace(certPassword))
                                {
                                    await _configService.StoreCertificatePasswordAsync(
                                        profile.Name, certPassword);
                                }
                            }
                        }
                        break;
                }

                await _configService.SaveProfileAsync(profile);
                AnsiConsole.MarkupLine($"\n[green]✓ Profile '{profile.Name}' created successfully![/]");
            }

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
            return 1;
        }
    }
}
