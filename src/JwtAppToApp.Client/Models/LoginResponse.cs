using System.Text.Json.Serialization;

namespace JwtAppToApp.Client.Models;

public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime? AccessTokenExpiryTime { get; set; }
    public string RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }


}

