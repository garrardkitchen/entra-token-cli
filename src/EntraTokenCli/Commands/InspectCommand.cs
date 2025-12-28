using System.ComponentModel;
using EntraAuthCli.UI;
using Spectre.Console.Cli;

namespace EntraAuthCli.Commands;

public class InspectSettings : CommandSettings
{
    [CommandArgument(0, "<TOKEN>")]
    [Description("JWT token to inspect (or '-' to read from stdin)")]
    public string Token { get; init; } = string.Empty;
}

public class InspectCommand : AsyncCommand<InspectSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, InspectSettings settings)
    {
        try
        {
            string token = settings.Token;

            // Read from stdin if token is '-'
            if (token == "-")
            {
                token = await Console.In.ReadToEndAsync();
                token = token.Trim();
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                ConsoleUi.DisplayError("No token provided.");
                return 1;
            }

            ConsoleUi.DisplayTokenClaims(token);

            return 0;
        }
        catch (Exception ex)
        {
            ConsoleUi.DisplayError($"Failed to inspect token: {ex.Message}");
            return 1;
        }
    }
}
