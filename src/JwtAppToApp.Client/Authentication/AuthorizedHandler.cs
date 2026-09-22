using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using JwtAppToApp.Client.Abstractions;

namespace JwtAppToApp.Client.Authentication;

public sealed class AuthorizedHandler : DelegatingHandler
{
    private readonly ITokenProvider _tokenProvider;

    public AuthorizedHandler(ITokenProvider tokenProvider)
    {
        _tokenProvider = tokenProvider
            ?? throw new ArgumentNullException(nameof(tokenProvider));
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await _tokenProvider.GetTokenAsync(cancellationToken).ConfigureAwait(false);
        SetBearerToken(request, token);

        // قبل از ارسال، یک کپی مستقل برای تلاش مجدد آماده می‌کنیم.
        using var retryRequest = await CloneHttpRequestAsync(request,cancellationToken).ConfigureAwait(false);

        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            return response;
        }

        string newToken;

        try
        {
            newToken = await _tokenProvider
                .ForceRefreshTokenAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch
        {
            response.Dispose();
            throw;
        }

        if (string.IsNullOrWhiteSpace(newToken))
        {
            return response;
        }

        response.Dispose();

        SetBearerToken(retryRequest, newToken);

        // فقط یک بار تلاش مجدد؛ حتی اگر پاسخ دوم نیز 401 باشد.
        return await base
            .SendAsync(retryRequest, cancellationToken)
            .ConfigureAwait(false);
    }

    private static void SetBearerToken(
        HttpRequestMessage request,
        string token)
    {
        request.Headers.Authorization = string.IsNullOrWhiteSpace(token)
            ? null
            : new AuthenticationHeaderValue("Bearer", token);
    }

    private static async Task<HttpRequestMessage> CloneHttpRequestAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var clone = new HttpRequestMessage(
            request.Method,
            request.RequestUri)
        {
            Version = request.Version,
            VersionPolicy = request.VersionPolicy
        };

        try
        {
            foreach (var header in request.Headers)
            {
                clone.Headers.TryAddWithoutValidation(
                    header.Key,
                    header.Value);
            }

            foreach (var option in request.Options)
            {
                clone.Options.Set(
                    new HttpRequestOptionsKey<object?>(option.Key),
                    option.Value);
            }

            if (request.Content is not null)
            {
                var contentBytes = await request.Content
                    .ReadAsByteArrayAsync(cancellationToken)
                    .ConfigureAwait(false);

                clone.Content = new ByteArrayContent(contentBytes);

                foreach (var header in request.Content.Headers)
                {
                    clone.Content.Headers.TryAddWithoutValidation(
                        header.Key,
                        header.Value);
                }
            }

            return clone;
        }
        catch
        {
            clone.Dispose();
            throw;
        }
    }
}
