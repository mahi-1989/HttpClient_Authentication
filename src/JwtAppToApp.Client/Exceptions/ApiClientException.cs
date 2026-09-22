using System;

namespace JwtAppToApp.Client.Exceptions;

public class ApiClientException : Exception
{
    public int? StatusCode { get; }

    public string? ResponseBody { get; }

    public ApiClientException(
        string message,
        int? statusCode = null,
        string? responseBody = null)
        : base(message)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}
