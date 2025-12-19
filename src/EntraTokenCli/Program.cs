using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;
using EntraTokenCli.Storage;
using EntraTokenCli.Configuration;
using EntraTokenCli.Authentication;
using EntraTokenCli.Commands;
using EntraTokenCli.Discovery;
using System.Reflection;

namespace EntraTokenCli;

class Program
{
    static async Task<int> Main(string[] args)
    {
        try
        {
            // Get version
            var version = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.
                InformationalVersion ?? "1.0.0";

            // Display version banner if help is requested or no args provided
            if (args.Length == 0 || args.Any(a => a == "--help" || a == "-h" || a == "help"))
            {
                AnsiConsole.Write(
                    new FigletText("entratool")
                        .LeftJustified()
                        .Color(Color.Cyan1));
                AnsiConsole.MarkupLine($"[dim]Version [orange1]{version}[/][/]");
                AnsiConsole.MarkupLine($"[dim]Created by [orange1]Garrard Kitchen[/] (garrardkitchen@gmail.com)[/]");
                AnsiConsole.WriteLine();
            }

            // Create service collection and configure DI
            var services = new ServiceCollection();
            ConfigureServices(services);

            // Create command app
            var app = new CommandApp(new TypeRegistrar(services));

            app.Configure(config =>
            {
                config.SetApplicationName("entratool");
                config.SetApplicationVersion(version);

                config.AddCommand<GetTokenCommand>("get-token")
                    .WithDescription("Generate an Azure AD access token")
                    .WithExample(new[] { "get-token", "-p", "myprofile" })
                    .WithExample(new[] { "get-token", "-p", "myprofile", "-f", "DeviceCode" })
                    .WithExample(new[] { "get-token", "-p", "myprofile", "-s", "https://management.azure.com/.default" })
                    .WithExample(new[] { "get-token", "-p", "myprofile", "-s", "api://my-api/.default" })
                    .WithExample(new[] { "get-token", "--no-clipboard" });

                config.AddCommand<RefreshCommand>("refresh")
                    .WithDescription("Refresh an existing token")
                    .WithExample(new[] { "refresh", "-p", "myprofile" });

                config.AddCommand<InspectCommand>("inspect")
                    .WithDescription("Inspect and decode a JWT token")
                    .WithExample(new[] { "inspect", "eyJ0eXAiOiJKV1QiLCJhbGci..." });

                config.AddCommand<DiscoverCommand>("discover")
                    .WithDescription("Discover Azure AD app registrations")
                    .WithExample(new[] { "discover", "-t", "contoso.onmicrosoft.com" })
                    .WithExample(new[] { "discover", "-t", "contoso.onmicrosoft.com", "-s", "MyApp*" })
                    .WithExample(new[] { "discover", "-s", "*Test*" });

                config.AddBranch("config", cfg =>
                {
                    cfg.SetDescription("Manage authentication profiles");

                    cfg.AddCommand<ConfigListCommand>("list")
                        .WithDescription("List all profiles");

                    cfg.AddCommand<ConfigCreateCommand>("create")
                        .WithDescription("Create a new profile");

                    cfg.AddCommand<ConfigEditCommand>("edit")
                        .WithDescription("Edit an existing profile")
                        .WithExample(new[] { "config", "edit", "-p", "myprofile" });

                    cfg.AddCommand<ConfigDeleteCommand>("delete")
                        .WithDescription("Delete a profile")
                        .WithExample(new[] { "config", "delete", "-p", "myprofile" });

                    cfg.AddCommand<ConfigExportCommand>("export")
                        .WithDescription("Export a profile")
                        .WithExample(new[] { "config", "export", "-p", "myprofile", "--include-secrets" })
                        .WithExample(new[] { "config", "export", "-p", "myprofile", "-o", "profile.enc" });

                    cfg.AddCommand<ConfigImportCommand>("import")
                        .WithDescription("Import a profile")
                        .WithExample(new[] { "config", "import", "-i", "profile.enc" })
                        .WithExample(new[] { "config", "import", "-i", "profile.enc", "-n", "newprofile" });
                });

                config.PropagateExceptions();
                config.ValidateExamples();
            });

            return await app.RunAsync(args);
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
            return 1;
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Register storage
        services.AddSingleton<ISecureStorage>(SecureStorageFactory.Create());

        // Register configuration services
        services.AddSingleton<ConfigService>();
        services.AddSingleton<ProfileExportService>();

        // Register authentication services
        services.AddSingleton<MsalAuthService>();

        // Register discovery services
        services.AddSingleton<AppRegistrationDiscoveryService>();

        // Register commands
        services.AddSingleton<DiscoverCommand>();
        services.AddSingleton<ProfileExportService>();

        // Register authentication services
        services.AddSingleton<MsalAuthService>();

        // Register commands
        services.AddSingleton<GetTokenCommand>();
        services.AddSingleton<RefreshCommand>();
        services.AddSingleton<InspectCommand>();
        services.AddSingleton<ConfigCommand>();
        services.AddSingleton<ConfigListCommand>();
        services.AddSingleton<ConfigCreateCommand>();
        services.AddSingleton<ConfigDeleteCommand>();
        services.AddSingleton<ConfigExportCommand>();
        services.AddSingleton<ConfigImportCommand>();
    }
}

/// <summary>
/// Type registrar for Spectre.Console.Cli dependency injection.
/// </summary>
internal sealed class TypeRegistrar : ITypeRegistrar
{
    private readonly IServiceCollection _services;

    public TypeRegistrar(IServiceCollection services)
    {
        _services = services;
    }

    public ITypeResolver Build()
    {
        return new TypeResolver(_services.BuildServiceProvider());
    }

    public void Register(Type service, Type implementation)
    {
        _services.AddSingleton(service, implementation);
    }

    public void RegisterInstance(Type service, object implementation)
    {
        _services.AddSingleton(service, implementation);
    }

    public void RegisterLazy(Type service, Func<object> factory)
    {
        _services.AddSingleton(service, _ => factory());
    }
}

/// <summary>
/// Type resolver for Spectre.Console.Cli dependency injection.
/// </summary>
internal sealed class TypeResolver : ITypeResolver
{
    private readonly IServiceProvider _provider;

    public TypeResolver(IServiceProvider provider)
    {
        _provider = provider;
    }

    public object? Resolve(Type? type)
    {
        return type == null ? null : _provider.GetService(type);
    }
}

