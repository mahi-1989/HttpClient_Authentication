# JwtAppToApp.NuGet

A production-style solution demonstrating how to build, pack, and consume a NuGet library:
**JwtAppToApp.Client** — JWT app-to-app (service-to-service) authentication client.

## Structure
- `src/JwtAppToApp.Client/` — the library (packaged as NuGet `JwtAppToApp.Client 1.0.0`)
- `sample/JwtAppToApp.Sample.Api/` — runnable ASP.NET Core Web API sample (token issuer + protected endpoints + IApiClient demo)
- `solution/` — notes and build scripts
- `START-HERE.md` — start reading here

## Build & Pack
```bash
dotnet restore JwtAppToApp.NuGet.sln
dotnet build  JwtAppToApp.NuGet.sln -c Release
dotnet pack   src/JwtAppToApp.Client/JwtAppToApp.Client.csproj -c Release -o ../nuget
```

## Consume
```csharp
builder.Services.AddJwtAppToAppClient(builder.Configuration);
// then inject IApiClient or ITokenProvider
```
