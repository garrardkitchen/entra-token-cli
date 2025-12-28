using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Spectre.Console;

namespace EntraAuthCli.Authentication;

/// <summary>
/// Service for loading X.509 certificates with flexible password handling.
/// </summary>
public class CertificateLoader
{
    /// <summary>
    /// Loads a certificate from a .pfx file with flexible password strategies.
    /// </summary>
    /// <param name="certificatePath">Path to the .pfx certificate file.</param>
    /// <param name="cachedPassword">Cached password from secure storage (if available).</param>
    /// <param name="useCachedPassword">Whether to use the cached password.</param>
    /// <param name="promptForPassword">Whether to prompt for password if needed.</param>
    /// <returns>The loaded X509Certificate2 instance.</returns>
    public static X509Certificate2 LoadCertificate(
        string certificatePath,
        string? cachedPassword,
        bool useCachedPassword,
        bool promptForPassword = true)
    {
        if (!File.Exists(certificatePath))
        {
            throw new FileNotFoundException($"Certificate file not found: {certificatePath}");
        }

        // Strategy 1: Try loading without password (passwordless certificate)
        try
        {
            return X509CertificateLoader.LoadPkcs12FromFile(certificatePath, null);
        }
        catch (CryptographicException)
        {
            // Certificate requires a password
        }

        // Strategy 2: Try cached password if available and user consented
        if (useCachedPassword && !string.IsNullOrWhiteSpace(cachedPassword))
        {
            try
            {
                return X509CertificateLoader.LoadPkcs12FromFile(certificatePath, cachedPassword);
            }
            catch (CryptographicException)
            {
                AnsiConsole.MarkupLine("[yellow]Cached certificate password is invalid or expired.[/]");
            }
        }

        // Strategy 3: Prompt for password
        if (promptForPassword)
        {
            while (true)
            {
                var password = AnsiConsole.Prompt(
                    new TextPrompt<string>($"Enter password for certificate [cyan]{Path.GetFileName(certificatePath)}[/]:")
                        .PromptStyle("yellow")
                        .Secret());

                try
                {
                    return X509CertificateLoader.LoadPkcs12FromFile(certificatePath, password);
                }
                catch (CryptographicException)
                {
                    AnsiConsole.MarkupLine("[red]Invalid certificate password. Please try again.[/]");
                }
            }
        }

        throw new InvalidOperationException(
            "Unable to load certificate. Password required but no password provided.");
    }

    /// <summary>
    /// Validates that a certificate file exists and is readable.
    /// </summary>
    public static bool ValidateCertificatePath(string certificatePath)
    {
        if (string.IsNullOrWhiteSpace(certificatePath))
        {
            return false;
        }

        if (!File.Exists(certificatePath))
        {
            return false;
        }

        var extension = Path.GetExtension(certificatePath).ToLowerInvariant();
        return extension == ".pfx" || extension == ".p12";
    }
}
