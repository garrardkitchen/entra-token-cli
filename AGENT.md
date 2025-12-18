# Agent Instructions - Entra Token CLI

This document provides guidance for AI coding agents working on the Entra Token CLI (entratool) project.

## Project Overview

**Purpose**: Cross-platform .NET 10 CLI tool for generating Microsoft Entra ID (formerly Azure AD) access tokens via multiple OAuth2 flows.

**Key Technologies**:
- .NET 10.0
- Spectre.Console (rich console UI)
- Microsoft.Identity.Client (MSAL for OAuth2)
- Microsoft.Graph (for app registration discovery)
- Platform-native secure storage (DPAPI, Keychain, XOR fallback)

**Target Platforms**: Windows (x64), macOS (ARM64, x64), Linux (x64)

## Project Structure

```
/Users/kitcheng/source/dotnet/access-token/
├── src/EntraTokenCli/
│   ├── Authentication/           # OAuth2 flows and token management
│   │   ├── MsalAuthService.cs   # Main authentication service (4 flows)
│   │   ├── CertificateLoader.cs # X.509 certificate handling
│   │   └── SecureTokenCache.cs  # MSAL token cache implementation
│   ├── Commands/                 # CLI command implementations
│   │   ├── GetTokenCommand.cs   # Primary token generation command
│   │   ├── RefreshCommand.cs    # Token refresh command
│   │   ├── InspectCommand.cs    # JWT inspection command
│   │   ├── ConfigCommand.cs     # Profile management (create/list/edit/delete/export/import)
│   │   └── DiscoverCommand.cs   # Azure app registration discovery
│   ├── Configuration/            # Profile and settings management
│   │   ├── AuthProfile.cs       # Profile data model (record type)
│   │   ├── ConfigService.cs     # Profile CRUD operations
│   │   └── ProfileExportService.cs # AES-256 encrypted export/import
│   ├── Discovery/                # Microsoft Graph integration
│   │   └── AppRegistrationDiscoveryService.cs # App registration search
│   ├── Storage/                  # Platform-specific secure storage
│   │   ├── ISecureStorage.cs    # Storage abstraction interface
│   │   ├── WindowsSecureStorage.cs # DPAPI implementation
│   │   ├── MacOsSecureStorage.cs   # Keychain CLI integration
│   │   ├── LinuxSecureStorage.cs   # XOR obfuscation fallback
│   │   └── SecureStorageFactory.cs # Platform detection factory
│   ├── UI/
│   │   └── ConsoleUi.cs         # Spectre.Console UI helpers
│   ├── Program.cs               # Entry point, DI setup, command registration
│   └── EntraTokenCli.csproj     # Project file
├── EntraTokenCli.sln            # Solution file
├── README.md                     # User documentation
├── CHANGELOG.md                  # Version history
├── CONTRIBUTING.md               # Contributor guidelines
├── LICENSE                       # MIT license
└── .github/workflows/
    └── release.yml              # CI/CD pipeline for releases
```

## Architecture Patterns

### 1. Command Pattern (Spectre.Console.Cli)
- All commands inherit from `AsyncCommand<TSettings>`
- Settings classes define command options using attributes
- Commands registered in `Program.cs` via `CommandApp<T>`

### 2. Dependency Injection
- Services registered in `Program.cs::ConfigureServices()`
- Custom `TypeRegistrar` and `TypeResolver` bridge DI with Spectre.Console
- Singleton pattern for stateless services (ConfigService, MsalAuthService)

### 3. Factory Pattern
- `SecureStorageFactory` provides platform-specific storage implementations
- Runtime OS detection via `RuntimeInformation.IsOSPlatform()`

### 4. Strategy Pattern
- OAuth2 flows selected at runtime based on profile/user choice
- Certificate password loading strategies (always prompt, cached, passwordless)

## Key Design Decisions

### Authentication Flow Inference
**Location**: `GetTokenCommand.cs` lines ~80-110

When no explicit flow is specified, the tool infers the appropriate OAuth2 flow from the authentication method:
- `ClientSecret` → `ClientCredentials` flow
- `Certificate` → `ClientCredentials` flow
- Otherwise → `InteractiveBrowser` flow

**Rationale**: Reduces friction for service principal authentication (most common use case).

### Lazy Profile Validation
Profiles are validated on-demand rather than at load time, allowing invalid profiles to exist temporarily without blocking the tool.

### Certificate Password Caching
Controlled by `--cache-cert-password` flag. When enabled, passwords stored in platform-native secure storage under key format: `entratool:{profileName}:cert-password`.

### Token Expiration Warnings
Default threshold: 5 minutes. Configurable via `--warn-expiry` option. Only applies to cached tokens, not fresh authentications.

## Important Conventions

### Naming
- **Tool name**: `entratool` (command-line invocation)
- **Package ID**: `EntraTokenCli` (NuGet package)
- **Namespace**: `EntraTokenCli.*` (all C# code)
- **Config directory**: `entratool` (filesystem paths)
- **Keychain service**: `entratool` (macOS Keychain)

### Error Handling
- Commands return `int` exit codes (0 = success, 1 = error)
- User-facing errors displayed via `ConsoleUi.DisplayError()`
- Exceptions caught at command level, not propagated to user
- Use `AnsiConsole.WriteException()` for debugging only

### UI Patterns
- Use `AnsiConsole.Prompt<T>()` for interactive input
- Use `ConsoleUi.ShowSpinnerAsync()` for long-running operations
- Color scheme: cyan (primary), green (success), yellow (warning), red (error)
- Always provide `[dim]` hints for complex prompts

### Secure Storage Keys
Format: `{serviceName}:{profileName}:{secretType}`
- Client secret: `entratool:myprofile:client-secret`
- Cert password: `entratool:myprofile:cert-password`

## Common Development Tasks

### Adding a New Command

1. Create command class in `Commands/` directory:
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

2. Register in `Program.cs`:
```csharp
config.AddCommand<MyCommand>("mycommand")
    .WithDescription("Command description")
    .WithExample(new[] { "mycommand", "-o", "value" });
```

### Adding a New OAuth2 Flow

1. Add enum value to `OAuth2Flow` in `AuthProfile.cs`
2. Implement flow in `MsalAuthService.AuthenticateAsync()` switch statement
3. Update flow inference logic in `GetTokenCommand.cs` if needed
4. Add documentation and examples to README.md

### Modifying Profile Schema

1. Update `AuthProfile` record in `Configuration/AuthProfile.cs`
2. Update validation in `ConfigService.ValidateProfileAsync()`
3. Update `ConfigCreateCommand` and `ConfigEditCommand` prompts
4. Consider backward compatibility for existing profiles
5. Update `ProfileExportService` if encryption format affected

### Platform-Specific Storage Changes

Each platform has its own implementation:
- **Windows**: Modify `WindowsSecureStorage.cs` (uses DPAPI)
- **macOS**: Modify `MacOsSecureStorage.cs` (calls `security` CLI)
- **Linux**: Modify `LinuxSecureStorage.cs` (XOR obfuscation)

All must implement `ISecureStorage` interface methods:
- `StoreAsync(key, value)`
- `RetrieveAsync(key)`
- `DeleteAsync(key)`
- `ExistsAsync(key)`

## Testing Guidelines

### Manual Testing Workflow

1. **Build**: `dotnet build --configuration Release`
2. **Run**: `dotnet run --project src/EntraTokenCli/EntraTokenCli.csproj -- <command>`
3. **Test profile creation**: `dotnet run -- config create`
4. **Test token generation**: `dotnet run -- get-token -p <profile-name>`
5. **Test on target platform** (important for platform-specific storage)

### Critical Test Scenarios

- **Client Credentials flow** with client secret (most common)
- **Authorization Code flow** with PKCE
- **Device Code flow** for headless environments
- **Certificate authentication** with password and passwordless
- **Profile export/import** with and without secrets
- **Clipboard integration** (verify fallback to file)
- **Token caching** and refresh behavior
- **Error handling** for invalid credentials, expired tokens
- **Cross-platform** storage (Windows DPAPI, macOS Keychain, Linux fallback)

## Build and Release

### Local Build
```bash
dotnet build --configuration Release
```

### Create Self-Contained Executables
```bash
dotnet publish -c Release -r win-x64 --self-contained -o ./publish/win-x64
dotnet publish -c Release -r osx-arm64 --self-contained -o ./publish/osx-arm64
dotnet publish -c Release -r osx-x64 --self-contained -o ./publish/osx-x64
dotnet publish -c Release -r linux-x64 --self-contained -o ./publish/linux-x64
```

### Release Process (GitHub Actions)

1. Create and push version tag: `git tag v0.1.0 && git push origin v0.1.0`
2. GitHub Actions workflow (`.github/workflows/release.yml`) automatically:
   - Builds for all platforms
   - Creates self-contained executables
   - Generates release notes
   - Publishes GitHub release with artifacts

### Version Management

- Update version in `src/EntraTokenCli/EntraTokenCli.csproj`
- Update `CHANGELOG.md` with new version section
- Follow Semantic Versioning: MAJOR.MINOR.PATCH

## Dependencies and NuGet Packages

**Core Dependencies**:
- `Spectre.Console` (0.49.1) - Rich console UI
- `Spectre.Console.Cli` - Command framework
- `Microsoft.Identity.Client` (4.66.2) - MSAL authentication
- `Microsoft.Graph` (5.66.0) - Graph API integration
- `Microsoft.Kiota.Abstractions` (1.15.2) - Required by Graph SDK
- `System.IdentityModel.Tokens.Jwt` (8.2.1) - JWT parsing
- `TextCopy` (6.2.1) - Cross-platform clipboard
- `System.Security.Cryptography.ProtectedData` (10.0.0) - Windows DPAPI
- `Microsoft.Extensions.DependencyInjection` (10.0.0) - DI container
- `Microsoft.Extensions.Hosting` (10.0.0) - Hosting abstractions

**Version Constraints**:
- Microsoft.Graph 5.66.0 requires Microsoft.Kiota.Abstractions >= 1.15.2
- Avoid package downgrades (will cause build errors)

## Known Platform-Specific Behaviors

### Windows
- DPAPI encryption scoped to current user
- Requires write access to `%APPDATA%\entratool\`

### macOS
- Keychain prompts may appear for first-time access
- Uses `security` command-line tool for Keychain operations
- Keychain items prefixed with `entratool:` for easy identification

### Linux
- No native secure storage, uses XOR obfuscation (weak security)
- Requires write access to `~/.config/entratool/secure/`
- libsecret integration is mentioned but not implemented (fallback only)

## Troubleshooting Common Issues

### Build Errors

**NU1605 Package Downgrade**: Ensure Microsoft.Kiota.Abstractions is >= 1.15.2

**CA1416 Platform-specific API**: Expected warnings for DPAPI usage in `WindowsSecureStorage.cs`, safe to ignore

### Runtime Issues

**Keychain Access Denied (macOS)**: User needs to grant terminal access in System Preferences

**Certificate Loading Fails**: Verify .pfx path and password, ensure X509CertificateLoader is used (.NET 10 compatible)

**Token Refresh Fails**: Re-authenticate with original flow, cached refresh token may have expired

## Security Considerations

- **Never log or display secrets** (client secrets, passwords, tokens in full)
- **Use secure storage** for all credentials (never plain text in profiles.json)
- **AES-256 + PBKDF2** for profile exports
- **Warn users** about clipboard usage in shared terminals
- **Token expiration** warnings enabled by default
- **Platform-native encryption** preferred over custom implementations

## Code Style and Patterns

- **Use record types** for immutable data models (AuthProfile)
- **Async/await** for all I/O operations
- **Nullable reference types** enabled, use null-checking patterns
- **Descriptive variable names** over comments
- **XML documentation** for public APIs
- **Color markup** in Spectre.Console: `[cyan]`, `[green]`, `[yellow]`, `[red]`, `[bold]`, `[dim]`

## Future Enhancements to Consider

- Implement proper libsecret support for Linux (replace XOR obfuscation)
- Add automated tests (unit and integration)
- Support for managed identity authentication
- Multiple tenant support per profile
- Token inspection with validation (signature verification)
- Profile templates for common scenarios
- Shell completion scripts (bash, zsh, pwsh)
- Logging framework for diagnostics

## References

- [Spectre.Console Documentation](https://spectreconsole.net/)
- [MSAL.NET Documentation](https://learn.microsoft.com/en-us/entra/msal/dotnet/)
- [Microsoft Graph SDK](https://learn.microsoft.com/en-us/graph/sdks/sdks-overview)
- [OAuth 2.0 Flows](https://oauth.net/2/)
- [Microsoft Entra ID](https://learn.microsoft.com/en-us/entra/fundamentals/)

---

**Last Updated**: 2025-12-18  
**Project Version**: 0.1.0
