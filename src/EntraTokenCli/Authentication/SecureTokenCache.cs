using EntraAuthCli.Storage;
using Microsoft.Identity.Client;

namespace EntraAuthCli.Authentication;

/// <summary>
/// Secure token cache implementation with encryption.
/// </summary>
public class SecureTokenCache
{
    private readonly ISecureStorage _secureStorage;
    private readonly string _cacheKey;

    public SecureTokenCache(ISecureStorage secureStorage, string cacheKey)
    {
        _secureStorage = secureStorage;
        _cacheKey = $"token-cache:{cacheKey}";
    }

    /// <summary>
    /// Registers cache serialization callbacks with MSAL.
    /// </summary>
    public void RegisterCache(ITokenCache tokenCache)
    {
        tokenCache.SetBeforeAccessAsync(BeforeAccessNotificationAsync);
        tokenCache.SetAfterAccessAsync(AfterAccessNotificationAsync);
    }

    private async Task BeforeAccessNotificationAsync(TokenCacheNotificationArgs args)
    {
        var cachedData = await _secureStorage.RetrieveAsync(_cacheKey);
        
        if (!string.IsNullOrWhiteSpace(cachedData))
        {
            try
            {
                var bytes = Convert.FromBase64String(cachedData);
                args.TokenCache.DeserializeMsalV3(bytes);
            }
            catch
            {
                // Ignore deserialization errors and start with empty cache
            }
        }
    }

    private async Task AfterAccessNotificationAsync(TokenCacheNotificationArgs args)
    {
        if (args.HasStateChanged)
        {
            var bytes = args.TokenCache.SerializeMsalV3();
            var base64 = Convert.ToBase64String(bytes);
            await _secureStorage.StoreAsync(_cacheKey, base64);
        }
    }

    /// <summary>
    /// Clears the token cache.
    /// </summary>
    public async Task ClearAsync()
    {
        await _secureStorage.DeleteAsync(_cacheKey);
    }
}
