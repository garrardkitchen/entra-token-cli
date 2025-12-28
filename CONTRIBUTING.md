# Contributing to Azure AD Token CLI

Thank you for your interest in contributing to Azure AD Token CLI! This document provides guidelines and instructions for contributing.

## Code of Conduct

Please be respectful and constructive in all interactions. We aim to maintain a welcoming and inclusive environment.

## Getting Started

### Prerequisites

- .NET 10.0 SDK or later
- Git
- An IDE or editor (Visual Studio, VS Code, Rider, etc.)

### Setting Up Development Environment

1. Fork the repository on GitHub
2. Clone your fork:
   ```bash
   git clone https://github.com/YOUR-USERNAME/entratokencli.git
   cd entratokencli
   ```

3. Add upstream remote:
   ```bash
   git remote add upstream https://github.com/ORIGINAL-OWNER/entratokencli.git
   ```

4. Restore dependencies:
   ```bash
   dotnet restore
   ```

5. Build the project:
   ```bash
   dotnet build
   ```

6. Run the CLI:
   ```bash
   dotnet run --project src/EntraTokenCli/EntraTokenCli.csproj -- --help
   ```

## Making Changes

### Branch Naming

- Feature branches: `feature/short-description`
- Bug fixes: `fix/short-description`
- Documentation: `docs/short-description`

Example:
```bash
git checkout -b feature/add-saml-support
```

### Code Style

- Follow standard C# coding conventions
- Use meaningful variable and method names
- Add XML documentation comments for public APIs
- Keep methods focused and reasonably sized
- Use `async/await` for I/O operations

### Project Structure

```
src/EntraTokenCli/
├── Authentication/      # MSAL integration and auth flows
├── Commands/            # CLI command implementations
├── Configuration/       # Profile and settings management
├── Discovery/           # Azure AD app registration discovery
├── Storage/            # Platform-specific secure storage
├── UI/                 # Console UI utilities
└── Program.cs          # Entry point and DI configuration
```

### Testing

Before submitting a PR:

1. Build in Release mode:
   ```bash
   dotnet build --configuration Release
   ```

2. Test on your platform:
   ```bash
   dotnet run --project src/EntraTokenCli/EntraTokenCli.csproj -- config create
   ```

3. Test key scenarios:
   - Profile creation and management
   - Token generation with different flows
   - Token inspection
   - Profile export/import

### Adding a New OAuth2 Flow

1. Add the flow to the `OAuth2Flow` enum in `Configuration/AuthProfile.cs`
2. Implement the flow method in `Authentication/MsalAuthService.cs`
3. Update `GetTokenCommand` to support the new flow
4. Add examples to README.md
5. Update CHANGELOG.md

### Adding a New Storage Provider

1. Implement `ISecureStorage` interface
2. Add platform detection in `SecureStorageFactory.cs`
3. Test on the target platform
4. Update documentation

## Submitting Changes

### Commit Messages

Write clear, descriptive commit messages:

```
Add Device Code flow timeout configuration

- Add --timeout option to get-token command
- Default timeout of 5 minutes
- Update help text and examples
```

Format:
- First line: Brief summary (50 chars or less)
- Blank line
- Detailed explanation of changes
- List key modifications with bullet points

### Pull Request Process

1. Update your fork:
   ```bash
   git fetch upstream
   git rebase upstream/main
   ```

2. Push your branch:
   ```bash
   git push origin feature/your-feature
   ```

3. Create a Pull Request on GitHub

4. In the PR description, include:
   - Summary of changes
   - Motivation and context
   - Type of change (feature, bug fix, docs, etc.)
   - Testing performed
   - Screenshots if UI changes
   - Related issues

5. Ensure CI checks pass

6. Respond to review feedback

### PR Checklist

- [ ] Code builds without errors or warnings
- [ ] Changes tested on at least one platform
- [ ] Documentation updated (README, CHANGELOG, code comments)
- [ ] Commit messages are clear and descriptive
- [ ] No sensitive information (tokens, secrets, passwords) in code
- [ ] New dependencies justified and minimal

## Reporting Issues

### Bug Reports

Include:
- Clear, descriptive title
- Steps to reproduce
- Expected vs actual behavior
- Version information (`entra-auth-cli --version`)
- Platform and OS version
- Relevant logs or error messages
- Screenshots if applicable

### Feature Requests

Include:
- Clear description of the feature
- Use case and motivation
- Proposed implementation (if any)
- Examples of usage

## Development Tips

### Debugging

Run with specific arguments:
```bash
dotnet run --project src/EntraTokenCli/EntraTokenCli.csproj -- get-token -p test
```

### Testing Secure Storage

Each platform has different secure storage:
- **Windows**: Check `%APPDATA%\entra-auth-cli\secure\`
- **macOS**: Use Keychain Access app to view entries
- **Linux**: Check `~/.config/entra-auth-cli/secure/`

### Testing Multi-Platform

Use Docker for Linux testing:
```bash
docker run -it --rm -v $(pwd):/app mcr.microsoft.com/dotnet/sdk:10.0 bash
cd /app
dotnet build
dotnet run --project src/EntraTokenCli/EntraTokenCli.csproj -- --help
```

### Performance Profiling

```bash
dotnet run --configuration Release --project src/EntraTokenCli/EntraTokenCli.csproj -- get-token -p test
```

## Release Process

(For maintainers only)

1. Update version in `EntraTokenCli.csproj`
2. Update CHANGELOG.md
3. Commit changes: `git commit -m "Release v1.x.x"`
4. Tag release: `git tag -a v1.x.x -m "Release v1.x.x"`
5. Push: `git push && git push --tags`
6. GitHub Actions will automatically build and publish

## Questions?

- Open a GitHub issue for questions
- Check existing issues and PRs first
- Be patient and respectful

## License

By contributing, you agree that your contributions will be licensed under the MIT License.
