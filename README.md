# Entra Token CLI (entratool)

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

- **Windows**: `entratool-win-x64.exe`
- **macOS (Apple Silicon)**: `entratool-osx-arm64`
- **macOS (Intel)**: `entratool-osx-x64`
- **Linux**: `entratool-linux-x64`

### Check Version

```bash
entratool --version
```

## Quick Start
- **macOS (Intel)**: `entratool-osx-x64`
- **Linux**: `entratool-linux-x64`

## Quick Start

### 1. Create a Profile

```bash
entratool config create
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
entratool get-token

# Using a specific profile
entratool get-token -p myprofile

# Using a specific OAuth2 flow
entratool get-token -p myprofile -f DeviceCode

# Without clipboard copy
entratool get-token -p myprofile --no-clipboard
```

### 3. Inspect a Token

```bash
# Inspect a token directly
entratool inspect eyJ0eXAiOiJKV1Qi...

# Inspect from stdin
echo "eyJ0eXAiOiJKV1Qi..." | entratool inspect -
```

### 4. Refresh a Token

```bash
entratool refresh -p myprofile
```

## Usage Examples

### Client Credentials Flow with Client Secret

```bash
# Create profile
entratool config create
# Profile name: myapp
# Tenant ID: contoso.onmicrosoft.com
# Client ID: <your-client-id>
# Scopes: https://graph.microsoft.com/.default
# Auth method: ClientSecret
# Client secret: ****

# Get token
entratool get-token -p myapp -f ClientCredentials
```

### Authorization Code Flow with Certificate

```bash
# Create profile with certificate
entratool config create
# Profile name: mycertapp
# Tenant ID: <tenant-id>
# Client ID: <client-id>
# Scopes: https://graph.microsoft.com/.default
# Auth method: Certificate
# Certificate path: /path/to/cert.pfx
# Cache certificate password: Yes
# Certificate password: ****

# Get token with cached certificate password
entratool get-token -p mycertapp -f AuthorizationCode --cache-cert-password
```

### Device Code Flow

```bash
entratool get-token -p myprofile -f DeviceCode
# Displays device code and URL for authentication
```

### Interactive Browser Flow

```bash
entratool get-token -p myprofile -f InteractiveBrowser

# With custom port
entratool get-token -p myprofile -f InteractiveBrowser --port 5000

# With custom redirect URI
entratool get-token -p myprofile --redirect-uri http://localhost:3000
```

## Profile Management

### List Profiles

```bash
entratool config list
```

### Export a Profile

```bash
# Export to stdout (for copying)
entratool config export -p myprofile

# Export to file with secrets included
entratool config export -p myprofile --include-secrets -o myprofile.enc
```

### Import a Profile

```bash
# Import from file
entratool config import -i myprofile.enc

# Import with new name
entratool config import -i myprofile.enc -n newprofile

# Import from clipboard (paste when prompted)
entratool config import
```

### Delete a Profile

```bash
entratool config delete -p myprofile
```

## Advanced Features

### Token Expiration Warnings

By default, `get-token` warns when returning cached tokens expiring within 5 minutes:

```bash
# Custom expiration warning threshold
entratool get-token -p myprofile --warn-expiry 10
```

### Certificate Password Strategies

1. **Always Prompt**: Secure, asks for password each time
   ```bash
   entratool get-token -p mycertapp
   ```

2. **Use Cached Password**: Convenient, retrieves from secure storage
   ```bash
   entratool get-token -p mycertapp --cache-cert-password
   ```

3. **Passwordless Certificate**: No password required
   ```bash
   # Configure profile with PasswordlessCertificate auth method
   ```

### Headless Environment Support

When clipboard is unavailable (SSH sessions, containers), tokens are automatically written to:
- **Windows**: `%APPDATA%\entratool\last-token.txt`
- **macOS/Linux**: `~/.config/entratool/last-token.txt`

```bash
# Explicit file output
entratool get-token -p myprofile --no-clipboard

# Read token from file
TOKEN=$(cat ~/.config/entratool/last-token.txt)
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

# Create .pfx file (for use with entratool)
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

### Using Certificates with entratool

Once uploaded, configure your profile:

```bash
# Create profile with certificate
entratool config create
# Profile name: myapp-cert
# Tenant ID: yourtenant.onmicrosoft.com
# Client ID: <your-client-id>
# Scopes: https://graph.microsoft.com/.default
# Auth method: Certificate
# Certificate path: /path/to/myapp.pfx
# Cache certificate password: Yes
# Certificate password: YourPassword123!

# Get token
entratool get-token -p myapp-cert -f ClientCredentials
```

### Important Notes

- **Upload `.cer` file to Azure** (public key only)
- **Keep `.pfx` file secure locally** (contains private key)
- **.pfx password** is stored in platform-native secure storage by entratool
- **Certificate expiration**: Self-signed certs typically 1-2 years; monitor expiration
- **Production**: Use proper CA-issued certificates or Azure Key Vault managed certificates

### Quick Test

After uploading:

```bash
# Test certificate authentication
entratool get-token -p myapp-cert -f ClientCredentials --cache-cert-password
```

## Configuration Files

### Profile Storage

Authentication profiles are stored in platform-specific locations:

- **Windows**: `%APPDATA%\entratool\profiles.json`
- **macOS/Linux**: `~/.config/entratool/profiles.json`

The `profiles.json` file contains profile metadata (tenant ID, client ID, scopes, certificate paths, etc.) but **does not contain secrets**.

Example location on macOS:
```bash
cat ~/.config/entratool/profiles.json
```

### Secure Storage

Secrets (client secrets and certificate passwords) are stored separately using platform-native secure storage:

- **Windows**: DPAPI (Data Protection API) - `%APPDATA%\entratool\secure\`
  - ✅ Strong encryption, scoped to current user
- **macOS**: Keychain - Service name: `entratool`, account format: `entratool:{profileName}:{secretType}`
  - ✅ Strong encryption, integrated with system security
  - Secrets can be viewed using Keychain Access app by searching for "entratool"
- **Linux**: XOR-obfuscated files (fallback) - `~/.config/entratool/secure/`
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

Ensure your user has permission to write to `%APPDATA%\entratool\`.

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
entratool get-token -p myprofile -f InteractiveBrowser
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
