namespace JwtAppToApp.Client.Options;

public sealed class JwtAppToAppOptions
{
    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string LoginUrl { get; set; } ="/api/authentication/login";

    public string BaseUrl { get; set; } = "http://airicapi.airic-ir.com/";
    public string RefreshTokenUrl { get; set; } = string.Empty;

   // public int ExpireMinutes { get; set; } = 5;

    public int ExpirationBufferSeconds { get; set; } = 0;

    // نامی که از زیر WEB_API_CREDENTIALS پیدا شده است
    public string CredentialSectionName { get; set; } =
        string.Empty;
}
