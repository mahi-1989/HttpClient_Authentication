using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using JwtAppToApp.Client.Abstractions;
using JwtAppToApp.Client.Exceptions;
using JwtAppToApp.Client.Options;
using Microsoft.Extensions.Options;

namespace JwtAppToApp.Client.Services;

public class ApiClient : IApiClient
{
    private readonly HttpClient _http;
    private readonly ITokenProvider _tokenProvider;

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public ApiClient(
        HttpClient http,
        ITokenProvider tokenProvider,
        IOptions<JwtAppToAppOptions> options)
    {
        _http = http;
        _tokenProvider = tokenProvider;

        if (_http.BaseAddress is null &&
            !string.IsNullOrWhiteSpace(options.Value.BaseUrl))
        {
            _http.BaseAddress = new Uri(options.Value.BaseUrl);
        }
    }

    public async Task<TResponse?> GetAsync<TResponse>(
        string path,
        CancellationToken ct = default)
    {
        using var request =
            await BuildRequestAsync(HttpMethod.Get, path, ct);

        return await SendAsync<TResponse>(request, ct);
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(
        string path,
        TRequest body,
        CancellationToken ct = default)
    {
        using var request =
            await BuildRequestAsync(HttpMethod.Post, path, ct);

        request.Content = JsonContent.Create(body);

        return await SendAsync<TResponse>(request, ct);
    }

    public async Task<TResponse?> PutAsync<TRequest, TResponse>(
        string path,
        TRequest body,
        CancellationToken ct = default)
    {
        using var request =
            await BuildRequestAsync(HttpMethod.Put, path, ct);

        request.Content = JsonContent.Create(body);

        return await SendAsync<TResponse>(request, ct);
    }

    public async Task DeleteAsync(
        string path,
        CancellationToken ct = default)
    {
        using var request =
            await BuildRequestAsync(HttpMethod.Delete, path, ct);

        await SendAsync(request, ct);
    }

    public async Task<TResponse?> DeleteAsync<TResponse>(
        string path,
        CancellationToken ct = default)
    {
        using var request =
            await BuildRequestAsync(HttpMethod.Delete, path, ct);

        return await SendAsync<TResponse>(request, ct);
    }

    private async Task<HttpRequestMessage> BuildRequestAsync(
        HttpMethod method,
        string path,
        CancellationToken ct)
    {
        var token = await _tokenProvider.GetTokenAsync(ct);

        var request = new HttpRequestMessage(method, path);

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        return request;
    }

    private async Task<TResponse?> SendAsync<TResponse>(
        HttpRequestMessage request,
        CancellationToken ct)
    {
        using var response = await _http.SendAsync(request, ct);

        await EnsureSuccessAsync(response, request, ct);

        // برای پاسخ‌های 204 یا پاسخ بدون Body
        var json = await response.Content.ReadAsStringAsync(ct);

        if (string.IsNullOrWhiteSpace(json))
        {
            return default;
        }

        return JsonSerializer.Deserialize<TResponse>(
            json,
            JsonOptions);
    }

    private async Task SendAsync(
        HttpRequestMessage request,
        CancellationToken ct)
    {
        using var response = await _http.SendAsync(request, ct);

        await EnsureSuccessAsync(response, request, ct);
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        HttpRequestMessage request,
        CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(ct);

        throw new ApiClientException(
            $"API call '{request.Method} {request.RequestUri}' failed " +
            $"with {(int)response.StatusCode}.",
            (int)response.StatusCode,
            body);
    }
}
