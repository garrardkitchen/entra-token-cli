# Entra Auth Cli (entra-auth-cli)

A cross-platform .NET CLI tool for generating Azure AD access tokens via multiple OAuth2 flows with secure storage, certificate authentication, and rich console UX.

## Features

- **Multiple OAuth2 Flows**: Authorization Code, Client Credentials, Device Code, Interactive Browser
- **Secure Storage**: Platform-native encryption (DPAPI on Windows, Keychain on macOS) ⚠️ *Linux uses XOR obfuscation only*
- **Certificate Authentication**: Support for .pfx certificates with flexible password handling
- **Profile Management**: Save and manage multiple authentication profiles
- **Profile Export/Import**: Share profiles across teams with AES-256 encryption
- **JWT Inspection**: Decode and display JWT token claims
- **Token Caching**: Automatic token caching with refresh support
- **Clipboard Integration**: Auto-copy tokens with headless environment detection
- **Rich Console UI**: Powered by Spectre.Console with colored output and spinners

## Installation

### Option 1: .NET Global Tool (Recommended)

```bash
dotnet tool install -g EntraTokenCli
```

### Option 2: Self-Contained Executables

Download the latest release for your platform from the [Releases](https://github.com/yourusername/entratokencli/releases) page:

- **Windows**: `entra-auth-cli-win-x64.exe`
- **macOS (Apple Silicon)**: `entra-auth-cli-osx-arm64`
- **macOS (Intel)**: `entra-auth-cli-osx-x64`
- **Linux**: `entra-auth-cli-linux-x64`

### Check Version

```bash
entra-auth-cli --version
```

## Quick Start
- **macOS (Intel)**: `entra-auth-cli-osx-x64`
- **Linux**: `entra-auth-cli-linux-x64`

## Quick Start

### 1. Create a Profile

```bash
entra-auth-cli config create
```

Follow the interactive prompts to configure:
- Profile name
- Tenant ID and Client ID
- Scopes (e.g., `https://graph.microsoft.com/.default`)
- Authentication method (Client Secret, Certificate, or Passwordless Certificate)
- Default OAuth2 flow (optional)

### 2. Generate a Token

```bash
# Using default/interactive profile selection
entra-auth-cli get-token

# Using a specific profile
entra-auth-cli get-token -p myprofile

# Using a specific OAuth2 flow
entra-auth-cli get-token -p myprofile -f DeviceCode

# Override scope for this request (useful for getting tokens for different APIs)
entra-auth-cli get-token -p myprofile -s "https://management.azure.com/.default"

# Get token for custom API
entra-auth-cli get-token -p myprofile -s "api://YOUR-API-CLIENT-ID/.default"

# Without clipboard copy
entra-auth-cli get-token -p myprofile --no-clipboard
```

### 3. Inspect a Token

```bash
# Inspect a token directly
entra-auth-cli inspect eyJ0eXAiOiJKV1Qi...

# Inspect from stdin
echo "eyJ0eXAiOiJKV1Qi..." | entra-auth-cli inspect -
```

### 4. Refresh a Token

```bash
entra-auth-cli refresh -p myprofile
```

## Usage Examples

### Client Credentials Flow with Client Secret

```bash
# Create profile
entra-auth-cli config create
# Profile name: myapp
# Tenant ID: contoso.onmicrosoft.com
# Client ID: <your-client-id>
# Scopes: https://graph.microsoft.com/.default
# Auth method: ClientSecret
# Client secret: ****

# Get token
entra-auth-cli get-token -p myapp -f ClientCredentials
```

### Authorization Code Flow with Certificate

```bash
# Create profile with certificate
entra-auth-cli config create
# Profile name: mycertapp
# Tenant ID: <tenant-id>
# Client ID: <client-id>
# Scopes: https://graph.microsoft.com/.default
# Auth method: Certificate
# Certificate path: /path/to/cert.pfx
# Cache certificate password: Yes
# Certificate password: ****

# Get token with cached certificate password
entra-auth-cli get-token -p mycertapp -f AuthorizationCode --cache-cert-password
```

### Device Code Flow

```bash
entra-auth-cli get-token -p myprofile -f DeviceCode
# Displays device code and URL for authentication
```

### Interactive Browser Flow

```bash
entra-auth-cli get-token -p myprofile -f InteractiveBrowser

# With custom port
entra-auth-cli get-token -p myprofile -f InteractiveBrowser --port 5000

# With custom redirect URI
entra-auth-cli get-token -p myprofile --redirect-uri http://localhost:3000
```

## Scope Management

### Understanding Scopes

Scopes define what resources and permissions your token will have access to. Common scope patterns:

- **Microsoft Graph API**: `https://graph.microsoft.com/.default`
- **Azure Management API**: `https://management.azure.com/.default`
- **Custom API (default scope)**: `api://YOUR-API-CLIENT-ID/.default`
- **Custom API (specific permission)**: `api://YOUR-API-CLIENT-ID/access`

### Setting Scopes in Profiles

When creating or editing a profile, you'll be prompted for scopes with helpful examples:

```bash
entra-auth-cli config create
# ... other prompts ...
# Examples shown:
#   - Microsoft Graph API: https://graph.microsoft.com/.default
#   - Azure Management API: https://management.azure.com/.default
#   - Custom API: api://YOUR-API-CLIENT-ID/.default
# Scopes (comma-separated): https://graph.microsoft.com/.default
```

### Overriding Scopes at Runtime

You can override the profile's scopes for a single token request using the `--scope` or `-s` option:

```bash
# Get token for Azure Management API (override profile scope)
entra-auth-cli get-token -p myprofile -s "https://management.azure.com/.default"

# Get token for custom API
entra-auth-cli get-token -p myprofile -s "api://12345678-1234-1234-1234-123456789abc/.default"

# Multiple scopes (comma-separated)
entra-auth-cli get-token -p myprofile -s "api://my-api/read,api://my-api/write"
```

### Use Case: Client App Calling API App

When you have a client app that needs to call a protected API:

1. **API App Registration**: Register your API in Entra ID and define app roles/scopes
2. **Client App Registration**: Register your client app with permissions to call the API
3. **Get Token**: Use the client app credentials to get a token scoped to the API:

```bash
# Configure client app profile with Graph scope (default)
entra-auth-cli config create
# Name: myclient
# Client ID: <client-app-id>
# Client Secret: <secret>
# Scopes: https://graph.microsoft.com/.default

# Get token for the API instead
entra-auth-cli get-token -p myclient -s "api://<api-app-id>/.default"
```

## Profile Management

### List Profiles

```bash
entra-auth-cli config list
```

### Export a Profile

```bash
# Export to stdout (for copying)
entra-auth-cli config export -p myprofile

# Export to file with secrets included
entra-auth-cli config export -p myprofile --include-secrets -o myprofile.enc
```

### Import a Profile

```bash
# Import from file
entra-auth-cli config import -i myprofile.enc

# Import with new name
entra-auth-cli config import -i myprofile.enc -n newprofile

# Import from clipboard (paste when prompted)
entra-auth-cli config import
```

### Delete a Profile

```bash
entra-auth-cli config delete -p myprofile
```

## Advanced Features

### Token Expiration Warnings

By default, `get-token` warns when returning cached tokens expiring within 5 minutes:

```bash
# Custom expiration warning threshold
entra-auth-cli get-token -p myprofile --warn-expiry 10
```

### Certificate Password Strategies

1. **Always Prompt**: Secure, asks for password each time
   ```bash
   entra-auth-cli get-token -p mycertapp
   ```

2. **Use Cached Password**: Convenient, retrieves from secure storage
   ```bash
   entra-auth-cli get-token -p mycertapp --cache-cert-password
   ```

3. **Passwordless Certificate**: No password required
   ```bash
   # Configure profile with PasswordlessCertificate auth method
   ```

### Headless Environment Support

When clipboard is unavailable (SSH sessions, containers), tokens are automatically written to:
- **Windows**: `%APPDATA%\entra-auth-cli\last-token.txt`
- **macOS/Linux**: `~/.config/entra-auth-cli/last-token.txt`

```bash
# Explicit file output
entra-auth-cli get-token -p myprofile --no-clipboard

# Read token from file
TOKEN=$(cat ~/.config/entra-auth-cli/last-token.txt)
```

## Working with Certificates

### Creating a Certificate for App Registration

#### Option 1: PowerShell (Windows/macOS/Linux with PowerShell)

```powershell
# Create a self-signed certificate
$cert = New-SelfSignedCertificate `
    -Subject "CN=MyAppName" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -KeyExportPolicy Exportable `
    -KeySpec Signature `
    -KeyLength 2048 `
    -KeyAlgorithm RSA `
    -HashAlgorithm SHA256 `
    -NotAfter (Get-Date).AddYears(2)

# Export the certificate with private key (.pfx)
$password = ConvertTo-SecureString -String "YourPassword123!" -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath "myapp.pfx" -Password $password

# Export the public key for Azure upload (.cer)
Export-Certificate -Cert $cert -FilePath "myapp.cer"
```

#### Option 2: OpenSSL (macOS/Linux)

```bash
# Generate private key and certificate
openssl req -x509 -newkey rsa:2048 -keyout myapp-key.pem -out myapp-cert.pem -days 730 -nodes \
    -subj "/CN=MyAppName"

# Create .pfx file (for use with entra-auth-cli)
openssl pkcs12 -export -out myapp.pfx -inkey myapp-key.pem -in myapp-cert.pem \
    -passout pass:YourPassword123!

# Create .cer file (for Azure upload)
openssl x509 -outform der -in myapp-cert.pem -out myapp.cer
```

#### Option 3: Azure Key Vault (Production)

```bash
# Create certificate in Key Vault (best for production)
az keyvault certificate create \
    --vault-name mykeyvault \
    --name myapp-cert \
    --policy @policy.json

# Download for local use
az keyvault secret download \
    --vault-name mykeyvault \
    --name myapp-cert \
    --encoding base64 \
    --file myapp.pfx
```

### Uploading Certificate to App Registration

#### Azure Portal

1. Navigate to **Azure Portal** → **Azure Active Directory** → **App registrations**
2. Select your application
3. Go to **Certificates & secrets** → **Certificates** tab
4. Click **Upload certificate**
5. Select your `.cer` file (public key only)
6. Add a description and click **Add**

#### Azure CLI

```bash
# Upload certificate to app registration
az ad app credential reset \
    --id <your-app-id> \
    --cert @myapp.cer \
    --append
```

#### PowerShell

```powershell
# Connect to Azure AD
Connect-AzureAD

# Upload certificate
$cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2("myapp.cer")
New-AzureADApplicationKeyCredential `
    -ObjectId <your-app-object-id> `
    -Type AsymmetricX509Cert `
    -Usage Verify `
    -Value $cert.GetRawCertData()
```

### Using Certificates with entra-auth-cli

Once uploaded, configure your profile:

```bash
# Create profile with certificate
entra-auth-cli config create
# Profile name: myapp-cert
# Tenant ID: yourtenant.onmicrosoft.com
# Client ID: <your-client-id>
# Scopes: https://graph.microsoft.com/.default
# Auth method: Certificate
# Certificate path: /path/to/myapp.pfx
# Cache certificate password: Yes
# Certificate password: YourPassword123!

# Get token
entra-auth-cli get-token -p myapp-cert -f ClientCredentials
```

### Important Notes

- **Upload `.cer` file to Azure** (public key only)
- **Keep `.pfx` file secure locally** (contains private key)
- **.pfx password** is stored in platform-native secure storage by entra-auth-cli
- **Certificate expiration**: Self-signed certs typically 1-2 years; monitor expiration
- **Production**: Use proper CA-issued certificates or Azure Key Vault managed certificates

### Quick Test

After uploading:

```bash
# Test certificate authentication
entra-auth-cli get-token -p myapp-cert -f ClientCredentials --cache-cert-password
```

## Configuration Files

### Profile Storage

Authentication profiles are stored in platform-specific locations:

- **Windows**: `%APPDATA%\entra-auth-cli\profiles.json`
- **macOS/Linux**: `~/.config/entra-auth-cli/profiles.json`

The `profiles.json` file contains profile metadata (tenant ID, client ID, scopes, certificate paths, etc.) but **does not contain secrets**.

Example location on macOS:
```bash
cat ~/.config/entra-auth-cli/profiles.json
```

### Secure Storage

Secrets (client secrets and certificate passwords) are stored separately using platform-native secure storage:

- **Windows**: DPAPI (Data Protection API) - `%APPDATA%\entra-auth-cli\secure\`
  - ✅ Strong encryption, scoped to current user
- **macOS**: Keychain - Service name: `entra-auth-cli`, account format: `entra-auth-cli:{profileName}:{secretType}`
  - ✅ Strong encryption, integrated with system security
  - Secrets can be viewed using Keychain Access app by searching for "entra-auth-cli"
- **Linux**: XOR-obfuscated files (fallback) - `~/.config/entra-auth-cli/secure/`
  - ⚠️ **SECURITY WARNING**: Uses XOR obfuscation, NOT cryptographic encryption
  - ⚠️ Secrets are easily reversible by anyone with file system access
  - ⚠️ Suitable for development only; avoid storing production credentials on Linux
  - 🔨 Proper libsecret integration is planned for future versions

**Linux Users - Important Security Considerations:**

The current Linux implementation provides **obfuscation only**, not true encryption. If you need to store sensitive production credentials on Linux, consider these alternatives:
- Use environment variables for secrets instead of profiles
- Store profiles on an encrypted file system
- Use a secrets manager (HashiCorp Vault, Azure Key Vault)
- Wait for libsecret integration in a future release

## Troubleshooting

### Windows: Access Denied Errors

Ensure your user has permission to write to `%APPDATA%\entra-auth-cli\`.

### macOS: Keychain Access Denied

Grant terminal/app access to Keychain in System Preferences → Security & Privacy.

### Linux: libsecret Unavailable

Install libsecret:
```bash
# Ubuntu/Debian
sudo apt-get install libsecret-1-dev

# Fedora/RHEL
sudo dnf install libsecret-devel
```

### Certificate Loading Fails

- Verify certificate path is correct
- Ensure .pfx file is not corrupted
- Check certificate password is correct

### Token Refresh Fails

```bash
# Re-authenticate using the original flow
entra-auth-cli get-token -p myprofile -f InteractiveBrowser
```

## Platform Requirements

- **.NET Runtime**: .NET 10.0 or later (for global tool)
- **Windows**: Windows 10+ (build 1607+)
- **macOS**: macOS 10.15+ (Catalina or later)
- **Linux**: Ubuntu 20.04+, Fedora 35+, or compatible distributions

## Development

### Build from Source

```bash
git clone https://github.com/yourusername/entratokencli.git
cd entratokencli
dotnet build
```

### Run Tests

```bash
dotnet test
```

### Publish

```bash
# Global tool
dotnet pack -c Release

# Self-contained executable (Windows)
dotnet publish -c Release -r win-x64 --self-contained

# Self-contained executable (macOS ARM)
dotnet publish -c Release -r osx-arm64 --self-contained

# Self-contained executable (Linux)
dotnet publish -c Release -r linux-x64 --self-contained
```

## Security Considerations

**Platform Security Status:**
- ✅ **Windows & macOS**: Strong cryptographic storage (DPAPI/Keychain)
- ⚠️ **Linux**: XOR obfuscation only - not suitable for production secrets

**Best Practices:**
- Client secrets and certificate passwords are stored in platform-native secure storage (Windows/macOS)
- **Linux users**: Avoid storing production credentials; use environment variables or secrets managers instead
- Profile exports are encrypted with AES-256 using PBKDF2 key derivation
- Tokens are cached with encryption using MSAL's built-in serialization
- Always use `--no-clipboard` in shared/recorded terminal sessions
- Regularly rotate client secrets and certificates
- Use passwordless certificates in secure environments when possible

## Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch
3. Make your changes with tests
4. Submit a pull request

## License

MIT License - see [LICENSE](LICENSE) file for details.

## Acknowledgments

- Built with [Microsoft.Identity.Client (MSAL)](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet)
- UI powered by [Spectre.Console](https://spectreconsole.net/)
- Clipboard integration via [TextCopy](https://github.com/CopyText/TextCopy)
