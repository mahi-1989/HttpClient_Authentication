namespace JwtAppToApp.Client.Models;

public sealed class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}