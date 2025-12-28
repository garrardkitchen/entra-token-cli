using System.IdentityModel.Tokens.Jwt;
using EntraAuthCli.Authentication;
using EntraAuthCli.Configuration;
using Spectre.Console;
using TextCopy;

namespace EntraAuthCli.UI;

/// <summary>
/// Console UI utilities using Spectre.Console.
/// </summary>
public static class ConsoleUi
{
    /// <summary>
    /// Displays a token with optional clipboard copy.
    /// </summary>
    public static async Task DisplayTokenAsync(
        AuthenticationResult result,
        bool copyToClipboard,
        string fallbackFilePath)
    {
        var panel = new Panel(new Markup($"[green]{result.AccessToken}[/]"))
        {
            Header = new PanelHeader("[bold yellow]Access Token[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Green)
        };

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();

        // Display token info
        var table = new Table()
            .BorderColor(Color.Grey)
            .AddColumn("Property")
            .AddColumn("Value");

        table.AddRow("Token Type", result.TokenType);
        table.AddRow("Expires On", result.ExpiresOn.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));
        table.AddRow("Scopes", string.Join(", ", result.Scopes));
        table.AddRow("From Cache", result.IsFromCache ? "[green]Yes[/]" : "[yellow]No[/]");

        var timeUntilExpiry = result.ExpiresOn - DateTimeOffset.UtcNow;
        var expiryStatus = timeUntilExpiry.TotalMinutes switch
        {
            <= 0 => "[red]Expired[/]",
            <= 5 => $"[red]Expires in {timeUntilExpiry.TotalMinutes:F1} minutes[/]",
            <= 15 => $"[yellow]Expires in {timeUntilExpiry.TotalMinutes:F1} minutes[/]",
            _ => $"[green]Expires in {timeUntilExpiry.TotalHours:F1} hours[/]"
        };
        table.AddRow("Status", expiryStatus);

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        // Handle clipboard/fallback
        if (copyToClipboard)
        {
            var copied = await TryCopyToClipboardAsync(result.AccessToken);
            if (copied)
            {
                AnsiConsole.MarkupLine("[green]✓ Token copied to clipboard[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[yellow]⚠ Clipboard unavailable, writing to file...[/]");
                await File.WriteAllTextAsync(fallbackFilePath, result.AccessToken);
                AnsiConsole.MarkupLine($"[green]✓ Token written to: {fallbackFilePath}[/]");
            }
        }
    }

    /// <summary>
    /// Displays JWT token claims in a formatted table.
    /// </summary>
    public static void DisplayTokenClaims(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        
        if (!handler.CanReadToken(token))
        {
            AnsiConsole.MarkupLine("[red]Invalid JWT token format[/]");
            return;
        }

        var jwtToken = handler.ReadJwtToken(token);

        // Header
        var headerTable = new Table()
            .BorderColor(Color.Blue)
            .AddColumn("[bold]Header Property[/]")
            .AddColumn("[bold]Value[/]");

        headerTable.AddRow("Algorithm", jwtToken.Header.Alg ?? "N/A");
        headerTable.AddRow("Type", jwtToken.Header.Typ ?? "N/A");
        if (jwtToken.Header.Kid != null)
        {
            headerTable.AddRow("Key ID", jwtToken.Header.Kid);
        }

        AnsiConsole.Write(new Panel(headerTable)
        {
            Header = new PanelHeader("[bold yellow]JWT Header[/]"),
            Border = BoxBorder.Rounded
        });
        AnsiConsole.WriteLine();

        // Claims
        var claimsTable = new Table()
            .BorderColor(Color.Green)
            .AddColumn("[bold]Claim Type[/]")
            .AddColumn("[bold]Value[/]");

        foreach (var claim in jwtToken.Claims.OrderBy(c => c.Type))
        {
            var value = claim.Value;
            if (value.Length > 100)
            {
                value = value.Substring(0, 97) + "...";
            }
            claimsTable.AddRow(claim.Type, value);
        }

        AnsiConsole.Write(new Panel(claimsTable)
        {
            Header = new PanelHeader("[bold yellow]JWT Claims[/]"),
            Border = BoxBorder.Rounded
        });
        AnsiConsole.WriteLine();

        // Token validity
        var now = DateTime.UtcNow;
        var validFrom = jwtToken.ValidFrom;
        var validTo = jwtToken.ValidTo;

        var validityTable = new Table()
            .BorderColor(Color.Yellow)
            .AddColumn("[bold]Property[/]")
            .AddColumn("[bold]Value[/]");

        validityTable.AddRow("Valid From", validFrom.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));
        validityTable.AddRow("Valid To", validTo.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));

        var isValid = now >= validFrom && now <= validTo;
        var status = isValid ? "[green]Valid[/]" : "[red]Invalid[/]";
        validityTable.AddRow("Status", status);

        if (isValid)
        {
            var timeUntilExpiry = validTo - now;
            validityTable.AddRow("Time Until Expiry", $"{timeUntilExpiry.TotalHours:F1} hours");
        }

        AnsiConsole.Write(new Panel(validityTable)
        {
            Header = new PanelHeader("[bold yellow]Token Validity[/]"),
            Border = BoxBorder.Rounded
        });
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Displays a list of profiles in an interactive selection prompt.
    /// </summary>
    public static AuthProfile PromptForProfile(IReadOnlyList<AuthProfile> profiles)
    {
        if (profiles.Count == 0)
        {
            throw new InvalidOperationException(
                "No profiles found. Create a profile first using 'entra-auth-cli config create'.");
        }

        if (profiles.Count == 1)
        {
            return profiles[0];
        }

        var selection = AnsiConsole.Prompt(
            new SelectionPrompt<AuthProfile>()
                .Title("[yellow]Select a profile:[/]")
                .PageSize(10)
                .AddChoices(profiles)
                .UseConverter(p => $"{p.Name} ({p.TenantId})")
        );

        return selection;
    }

    /// <summary>
    /// Displays an error message.
    /// </summary>
    public static void DisplayError(string message)
    {
        AnsiConsole.Write(new Panel(new Markup($"[red]{message}[/]"))
        {
            Header = new PanelHeader("[bold red]Error[/]"),
            Border = BoxBorder.Heavy,
            BorderStyle = new Style(Color.Red)
        });
    }

    /// <summary>
    /// Displays a success message.
    /// </summary>
    public static void DisplaySuccess(string message)
    {
        AnsiConsole.Write(new Panel(new Markup($"[green]{message}[/]"))
        {
            Header = new PanelHeader("[bold green]Success[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Green)
        });
    }

    /// <summary>
    /// Displays a warning message.
    /// </summary>
    public static void DisplayWarning(string message)
    {
        AnsiConsole.Write(new Panel(new Markup($"[yellow]{message}[/]"))
        {
            Header = new PanelHeader("[bold yellow]Warning[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Yellow)
        });
    }

    /// <summary>
    /// Tries to copy text to clipboard with headless environment detection.
    /// </summary>
    private static async Task<bool> TryCopyToClipboardAsync(string text)
    {
        try
        {
            // Check for headless environment
            if (IsHeadlessEnvironment())
            {
                return false;
            }

            await ClipboardService.SetTextAsync(text);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Detects if running in a headless environment.
    /// </summary>
    private static bool IsHeadlessEnvironment()
    {
        // On Linux, check for DISPLAY environment variable
        if (OperatingSystem.IsLinux())
        {
            var display = Environment.GetEnvironmentVariable("DISPLAY");
            if (string.IsNullOrWhiteSpace(display))
            {
                return true;
            }
        }

        // On Windows, check for console window handle
        if (OperatingSystem.IsWindows())
        {
            try
            {
                // If we can't get console info, likely headless
                return Console.WindowWidth == 0;
            }
            catch
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Shows a spinner while executing an async operation.
    /// </summary>
    public static async Task<T> ShowSpinnerAsync<T>(
        string message,
        Func<Task<T>> operation)
    {
        return await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("green bold"))
            .StartAsync(message, async ctx => await operation());
    }
}
