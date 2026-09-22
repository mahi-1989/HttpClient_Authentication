using System.Net.Http.Json;
using System.Text.Json;
using JwtAppToApp.Client.Abstractions;
using JwtAppToApp.Client.Models;
using JwtAppToApp.Client.Options;

namespace JwtAppToApp.Client.Services;

public sealed class TokenProvider : ITokenProvider, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly JwtAppToAppOptions _options;

    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    private TokenCacheEntry? _cachedToken;

    private bool _disposed;

    public TokenProvider(
        HttpClient httpClient,
        JwtAppToAppOptions options)
    {
        _httpClient = httpClient
            ?? throw new ArgumentNullException(nameof(httpClient));

        _options = options
            ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<string> GetTokenAsync(
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var cachedToken = _cachedToken;

        if (cachedToken is not null &&
            !cachedToken.IsExpired(
                _options.ExpirationBufferSeconds))
        {
            return cachedToken.AccessToken;
        }

        await _tokenLock.WaitAsync(cancellationToken);

        try
        {
            // ممکن است Thread دیگری در فاصله‌ی انتظار Lock
            // توکن را Refresh یا Login کرده باشد.
            cachedToken = _cachedToken;

            if (cachedToken is not null &&
                !cachedToken.IsExpired(
                    _options.ExpirationBufferSeconds))
            {
                return cachedToken.AccessToken;
            }

            // ابتدا Refresh Token
            if (cachedToken is not null &&
                !string.IsNullOrWhiteSpace(
                    cachedToken.RefreshToken))
            {
                var refreshedToken =
                    await TryRefreshTokenCoreAsync(
                        cachedToken,
                        cancellationToken);

                if (refreshedToken is not null)
                {
                    _cachedToken = refreshedToken;

                    return refreshedToken.AccessToken;
                }
            }

            // اگر Refresh ناموفق بود، Login کامل انجام می‌شود.
            var loggedInToken =
                await LoginCoreAsync(cancellationToken);

            _cachedToken = loggedInToken;

            return loggedInToken.AccessToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    public async Task<string> ForceRefreshTokenAsync(
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        await _tokenLock.WaitAsync(cancellationToken);

        try
        {
            var currentToken = _cachedToken;

            // اگر Refresh Token موجود است، ابتدا Refresh انجام شود.
            if (currentToken is not null &&
                !string.IsNullOrWhiteSpace(
                    currentToken.RefreshToken))
            {
                var refreshedToken =
                    await TryRefreshTokenCoreAsync(
                        currentToken,
                        cancellationToken);

                if (refreshedToken is not null)
                {
                    _cachedToken = refreshedToken;

                    return refreshedToken.AccessToken;
                }
            }

            // در صورت نبودن یا نامعتبر بودن Refresh Token
            // Login کامل انجام می‌شود.
            var loggedInToken =
                await LoginCoreAsync(cancellationToken);

            _cachedToken = loggedInToken;

            return loggedInToken.AccessToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    public void Invalidate()
    {
        _cachedToken = null;
    }

    private async Task<TokenCacheEntry?> TryRefreshTokenCoreAsync(
        TokenCacheEntry currentToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(
                _options.RefreshTokenUrl))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(
                currentToken.RefreshToken))
        {
            return null;
        }

        var request = new RefreshTokenRequest
        {
            RefreshToken = currentToken.RefreshToken
        };

        using var response =
            await _httpClient.PostAsJsonAsync(
                _options.RefreshTokenUrl,
                request,
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var result =
            await response.Content.ReadFromJsonAsync<LoginResponse>(
                cancellationToken: cancellationToken);

        if (result is null ||
            string.IsNullOrWhiteSpace(result.AccessToken))
        {
            return null;
        }

        return new TokenCacheEntry
        {
            AccessToken = result.AccessToken,

            // پشتیبانی از Refresh Token Rotation
            RefreshToken =
                string.IsNullOrWhiteSpace(result.RefreshToken)
                    ? currentToken.RefreshToken
                    : result.RefreshToken,

            AccessTokenExpiryTime = result.AccessTokenExpiryTime,
            RefreshTokenExpiryTime= result.RefreshTokenExpiryTime,
        };
    }

    private async Task<TokenCacheEntry> LoginCoreAsync(
        CancellationToken cancellationToken)
    {
        var request = new LoginRequest
        {
            UserName = _options.UserName,
            Password = _options.Password
        };

        using var response =
            await _httpClient.PostAsJsonAsync(
                _options.LoginUrl,
                request,
                cancellationToken);

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content.ReadFromJsonAsync<LoginResponse>(
                cancellationToken: cancellationToken);

        if (result is null ||
            string.IsNullOrWhiteSpace(result.AccessToken))
        {
            throw new InvalidOperationException(
                "Login response does not contain a valid access token.");
        }

        return new TokenCacheEntry
        {
            AccessToken = result.AccessToken,
            RefreshToken = result.RefreshToken,
            AccessTokenExpiryTime = result.AccessTokenExpiryTime,
            RefreshTokenExpiryTime =result.RefreshTokenExpiryTime,
        };
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _tokenLock.Dispose();
    }

    private sealed class TokenCacheEntry
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTime? AccessTokenExpiryTime { get; set; }
        public string RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
        //public bool IsExpired(int bufferSeconds)
        //{
        //    return AccessTokenExpiryTime <= DateTimeOffset.UtcNow.AddSeconds(bufferSeconds);
        //}
        public bool IsExpired(int bufferSeconds)
        {
            return !AccessTokenExpiryTime.HasValue ||
                   AccessTokenExpiryTime.Value <= DateTime.UtcNow.AddSeconds(bufferSeconds);
        }

    }
}

