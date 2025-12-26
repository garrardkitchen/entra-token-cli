# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Entra Token CLI (entratool)** - A cross-platform .NET 10 CLI tool for generating Microsoft Entra ID access tokens via multiple OAuth2 flows. Built with Spectre.Console for rich terminal UX and MSAL for authentication.

## Build and Development Commands

### Build
```bash
dotnet build --configuration Release
```

### Run Locally
```bash
dotnet run --project src/EntraTokenCli/EntraTokenCli.csproj -- <command>

# Examples:
dotnet run -- config create
dotnet run -- get-token -p myprofile
dotnet run -- inspect <token>
```

### Test Manually
```bash
# Create a test profile
dotnet run -- config create

# Get token with profile
dotnet run -- get-token -p <profile-name>

# Test different flows
dotnet run -- get-token -p <profile> -f ClientCredentials
dotnet run -- get-token -p <profile> -f DeviceCode
dotnet run -- get-token -p <profile> -f InteractiveBrowser
```

### Publish Self-Contained Executables
```bash
dotnet publish -c Release -r win-x64 --self-contained -o ./publish/win-x64
dotnet publish -c Release -r osx-arm64 --self-contained -o ./publish/osx-arm64
dotnet publish -c Release -r osx-x64 --self-contained -o ./publish/osx-x64
dotnet publish -c Release -r linux-x64 --self-contained -o ./publish/linux-x64
```

### Create NuGet Package
```bash
dotnet pack -c Release
```

## Architecture

### Command Pattern with Spectre.Console.Cli
- All commands inherit from `AsyncCommand<TSettings>`
- Command settings classes use attributes (`[CommandOption]`, `[Description]`)
- Commands registered in `Program.cs::Main()` via `CommandApp`
- DI integration via custom `TypeRegistrar` and `TypeResolver` classes

### Key Services
- **MsalAuthService** (`Authentication/MsalAuthService.cs`) - Core OAuth2 authentication with 4 flows:
  - ClientCredentials (service principal)
  - AuthorizationCode with PKCE
  - DeviceCode (headless environments)
  - InteractiveBrowser (local web server)

- **ConfigService** (`Configuration/ConfigService.cs`) - Profile CRUD operations (JSON storage)

- **SecureStorageFactory** (`Storage/SecureStorageFactory.cs`) - Platform detection factory:
  - Windows: DPAPI encryption (`WindowsSecureStorage.cs`)
  - macOS: Keychain via `security` CLI (`MacOsSecureStorage.cs`)
  - Linux: XOR obfuscation fallback (`LinuxSecureStorage.cs`)

- **ProfileExportService** (`Configuration/ProfileExportService.cs`) - AES-256 encrypted export/import

### OAuth2 Flow Inference Logic
When user doesn't specify `-f/--flow`, the tool infers from the profile's auth method:
- `ClientSecret` → `ClientCredentials`
- `Certificate` or `PasswordlessCertificate` → `ClientCredentials`
- Otherwise → `InteractiveBrowser`

Location: `GetTokenCommand.cs` lines ~80-110

### Data Storage Locations
- **Profiles**: `~/.config/entratool/profiles.json` (macOS/Linux) or `%APPDATA%\entratool\profiles.json` (Windows)
- **Secrets**: Platform-native secure storage with keys: `entratool:{profileName}:{secretType}`
- **Token cache**: MSAL's built-in cache (encrypted)
- **Last token fallback**: `~/.config/entratool/last-token.txt` when clipboard unavailable

## Important Patterns

### Error Handling
- Commands return `int` exit codes (0 = success, 1 = error)
- User-facing errors use `ConsoleUi.DisplayError()` (defined in `UI/ConsoleUi.cs`)
- Never propagate exceptions to users; catch at command level

### Console UI Conventions
- Use `AnsiConsole.Prompt<T>()` for interactive input
- Use `ConsoleUi.ShowSpinnerAsync()` for async operations
- Color scheme: `[cyan]` (primary), `[green]` (success), `[yellow]` (warning), `[red]` (error)
- Add `[dim]` hints for complex prompts

### Certificate Handling
- Use `X509CertificateLoader` (.NET 10+ API) in `CertificateLoader.cs`
- Support three modes: always prompt, cached password, passwordless
- Certificate passwords stored as `entratool:{profileName}:cert-password`

### Scope Management
- Profiles store default scopes
- Runtime override via `--scope/-s` option
- Common patterns: `https://graph.microsoft.com/.default`, `api://{clientId}/.default`

## Naming Conventions

- **CLI command**: `entratool` (user invokes this)
- **Package ID**: `EntraTokenCli` (NuGet package name)
- **Namespace**: `EntraTokenCli.*` (all C# code)
- **Config directory**: `entratool` (filesystem storage)
- **Keychain service**: `entratool` (macOS Keychain service name)

## Platform-Specific Notes

### Windows
- DPAPI scoped to current user
- Secure storage in `%APPDATA%\entratool\secure\`

### macOS
- Keychain integration via `security` CLI tool
- Keychain items searchable by prefix: `entratool:`
- May prompt for keychain access on first use

### Linux
- **Security Warning**: XOR obfuscation only, not cryptographic encryption
- Fallback storage in `~/.config/entratool/secure/`
- Not suitable for production secrets

## Dependencies

Core packages:
- `Spectre.Console` (0.49.1) - Rich console UI and command framework
- `Microsoft.Identity.Client` (4.66.2) - MSAL authentication
- `Microsoft.Graph` (5.66.0) - App registration discovery
- `Microsoft.Kiota.Abstractions` (1.15.2) - Required by Graph SDK
- `System.IdentityModel.Tokens.Jwt` (8.2.1) - JWT parsing
- `TextCopy` (6.2.1) - Cross-platform clipboard
- `System.Security.Cryptography.ProtectedData` (10.0.0) - Windows DPAPI

**Important**: Microsoft.Graph 5.66.0 requires Microsoft.Kiota.Abstractions >= 1.15.2. Avoid package downgrades.

## Adding New Commands

1. Create command class in `Commands/` directory
2. Define settings class inheriting `CommandSettings`
3. Implement `AsyncCommand<TSettings>`
4. Inject services via constructor
5. Register in `Program.cs::ConfigureServices()` and `app.Configure()`

Example structure:
```csharp
public class MyCommandSettings : CommandSettings
{
    [CommandOption("-o|--option")]
    [Description("Option description")]
    public string? Option { get; init; }
}

public class MyCommand : AsyncCommand<MyCommandSettings>
{
    private readonly ConfigService _configService;

    public MyCommand(ConfigService configService)
    {
        _configService = configService;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, MyCommandSettings settings)
    {
        try
        {
            // Implementation
            return 0;
        }
        catch (Exception ex)
        {
            ConsoleUi.DisplayError(ex.Message);
            return 1;
        }
    }
}
```

## Modifying Profile Schema

When changing `AuthProfile` record in `Configuration/AuthProfile.cs`:
1. Update the record definition
2. Update validation in `ConfigService.ValidateProfileAsync()`
3. Update interactive prompts in `ConfigCreateCommand` and `ConfigEditCommand`
4. Consider backward compatibility for existing profiles
5. Test export/import functionality

## Version Management

- Version defined in `src/EntraTokenCli/EntraTokenCli.csproj` `<Version>` tag
- Displayed via `--version` flag and in help banner
- Update `CHANGELOG.md` for each release
- Follow Semantic Versioning: MAJOR.MINOR.PATCH

## Release Process

1. Update version in `.csproj` file
2. Update `CHANGELOG.md`
3. Create git tag: `git tag v0.1.0`
4. Push tag: `git push origin v0.1.0`
5. GitHub Actions (`.github/workflows/release.yml`) automatically builds and publishes release

## Code Style

- Use record types for immutable data models (e.g., `AuthProfile`)
- Enable nullable reference types (`<Nullable>enable</Nullable>`)
- Async/await for all I/O operations
- XML documentation for public APIs
- Descriptive variable names over excessive comments
