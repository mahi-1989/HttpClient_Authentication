# JwtAppToApp.Client

کتابخانه‌ی قابل استفاده مجدد (NuGet) برای ارتباط app-to-app با احراز هویت JWT.

## چه چیزی داخل این پکیج است؟
- `Abstractions/IApiClient.cs` و `Abstractions/ITokenProvider.cs`
- `Authentication/JwtHandler.cs` (اضافه‌کردن Bearer token)
- `Authentication/AuthorizedHandler.cs` (invalidate توکن در صورت 401)
- `Models/LoginRequest.cs` و `Models/LoginResponse.cs`
- `Options/JwtAppToAppOptions.cs`
- `Services/TokenProvider.cs` و `Services/ApiClient.cs`
- `Exceptions/ApiClientException.cs`
- `Extensions/ServiceCollectionExtensions.cs`

## چه چیزی داخل این پکیج **نیست**؟
- هیچ Controller یا View یا ViewModel مخصوص پروژه‌ی شما
- هیچ secret واقعی
- سرویس‌های دامنه‌ای مثل `CompanyService` (این‌ها در پروژه‌ی sample هستند)

## Build و Pack
```bash
dotnet restore src/JwtAppToApp.Client/JwtAppToApp.Client.csproj
dotnet build  src/JwtAppToApp.Client/JwtAppToApp.Client.csproj -c Release
dotnet pack   src/JwtAppToApp.Client/JwtAppToApp.Client.csproj -c Release -o ./artifacts
```

## ساخت local NuGet source و نصب
```bash
dotnet nuget add source "$(pwd)/artifacts" --name local
dotnet add package JwtAppToApp.Client --version 1.0.0 --source local
```

## تنظیم appsettings
```json
{
  "WEB_API_CREDENTIALS": {
    "BaseUrl": "https://your-api.example.com",
    "LoginPath": "/api/auth/login",
    "ClientId": "your-client-id",
    "ClientSecret": "<from user-secrets or env>",
    "TimeoutSeconds": 30,
    "Validate": true
  }
}
```

## استفاده در Program.cs
```csharp
builder.Services.AddJwtAppToAppClient(
    builder.Configuration.GetSection("WEB_API_CREDENTIALS"));
```

## هشدار secret
هرگز `ClientSecret` واقعی را در سورس یا `appsettings.json` commit نکنید.
از `dotnet user-secrets` یا environment variables استفاده کنید.

## نسخه‌گذاری
از Semantic Versioning استفاده کنید: `1.0.0` → `1.0.1` (bugfix)، `1.1.0` (feature)، `2.0.0` (breaking).
