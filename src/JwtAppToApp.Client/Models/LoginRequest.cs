using System.Text.Json.Serialization;

namespace JwtAppToApp.Client.Models;

public class LoginRequest
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

}
