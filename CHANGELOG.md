# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.2.2] - 2026-01-09

### Fixed
- Fixed `refresh` command to support ClientCredentials flow with ClientSecret and Certificate authentication
- Fixed `refresh` command failing with "No cached account found" error for service principal profiles
- Fixed token cache detection to accurately show "From Cache" status using MSAL's TokenSource metadata
- Added `.WithForceRefresh(true)` to ensure tokens are regenerated with updated API permissions and role claims
- Added profile validation to `RefreshTokenAsync` for clearer error messages on misconfigured profiles

### Changed
- Enhanced `RefreshCommand` to pre-load certificates before operations to avoid password prompts during spinner
- Improved token refresh behavior to consistently generate fresh tokens with full expiration time

## [1.0.0] - 2025-12-17

### Added
- Initial release of Entra Auth Cli
- Multiple OAuth2 flow support (Authorization Code, Client Credentials, Device Code, Interactive Browser)
- Platform-specific secure storage (DPAPI on Windows, Keychain on macOS, libsecret on Linux)
- Certificate-based authentication with flexible password handling
- Profile management with create/edit/delete/list operations
- Profile export/import with AES-256 encryption for team sharing
- JWT token inspection with claim display
- Token caching with automatic refresh support
- Clipboard integration with headless environment detection
- Rich console UI powered by Spectre.Console
- Azure app registration discovery with auto-consent
- Cross-platform support (Windows, macOS, Linux)
- NuGet global tool distribution
- Self-contained executable builds for all platforms
- Comprehensive documentation and examples

### Features
- `get-token` - Generate access tokens with customizable flows
- `refresh` - Force generation of new tokens to pick up updated permissions
- `inspect` - Decode and display JWT token claims
- `config create` - Interactive profile creation
- `config list` - Display all saved profiles
- `config delete` - Remove profiles and associated secrets
- `config export` - Export profiles with optional secrets
- `config import` - Import profiles from encrypted data
- `discover` - Search Microsoft Entra ID app registrations with wildcard support

### Security
- Client secrets stored in platform-native secure storage
- Certificate passwords optionally cached with user consent
- Profile exports encrypted with AES-256 and PBKDF2
- Token cache encrypted using MSAL serialization
- No plaintext secrets in configuration files

### Developer Experience
- Colored console output with status indicators
- Spinner animations for long-running operations
- Interactive prompts with arrow-key navigation
- Comprehensive help text and examples
- Smart port selection for localhost redirect URIs
- Automatic fallback to file output when clipboard unavailable
- Token expiration warnings with configurable thresholds

[0.2.2]: https://github.com/garrardkitchen/entratokencli/releases/tag/v0.2.2
[1.0.0]: https://github.com/garrardkitchen/entratokencli/releases/tag/v1.0.0
