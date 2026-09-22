namespace JwtAppToApp.Client.Abstractions;

/// <summary>
/// Typed client that automatically attaches JWT bearer tokens.
/// </summary>
public interface IApiClient
{
    Task<TResponse?> GetAsync<TResponse>(
        string path,
        CancellationToken ct = default);

    Task<TResponse?> PostAsync<TRequest, TResponse>(
        string path,
        TRequest body,
        CancellationToken ct = default);

    Task<TResponse?> PutAsync<TRequest, TResponse>(
        string path,
        TRequest body,
        CancellationToken ct = default);

    /// <summary>
    /// Sends a DELETE request when the API does not return a response body.
    /// </summary>
    Task DeleteAsync(
        string path,
        CancellationToken ct = default);

    /// <summary>
    /// Sends a DELETE request when the API returns a JSON response.
    /// </summary>
    Task<TResponse?> DeleteAsync<TResponse>(
        string path,
        CancellationToken ct = default);
}
