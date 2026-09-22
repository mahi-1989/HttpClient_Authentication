# START-HERE ✅

Welcome! This is a complete, ready-to-pack sample of a NuGet library.

## 1. What is inside
| Path | What it is |
|---|---|
| `src/JwtAppToApp.Client/` | The NuGet library: JWT token acquisition + caching + typed HTTP client |
| `sample/JwtAppToApp.Sample.Api/` | Sample Web API that issues JWTs, protects endpoints, and uses `IApiClient` |
| `solution/` | Build scripts (`build.sh`, `build.cmd`) and notes |
| `JwtAppToApp.NuGet.sln` | Visual Studio / dotnet solution containing both projects |

## 2. Run the sample
```bash
dotnet run --project sample/JwtAppToApp.Sample.Api
# open https://localhost:5001/swagger  (add Swagger if you like)
```

## 3. Get a token & call the API
```bash
curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"clientId":"service-a","clientSecret":"secret-123"}'

curl https://localhost:5001/api/data \
  -H "Authorization: Bearer <TOKEN>"
```

## 4. Pack the library
```bash
./solution/build.sh          # Linux/macOS
solution\build.cmd           # Windows
# Output: nuget/JwtAppToApp.Client.1.0.0.nupkg
```

## 5. Install the packed package
```bash
dotnet add package JwtAppToApp.Client -s ./nuget
```

## Key classes
- `TokenProvider` — requests and caches tokens (thread-safe, auto-refresh with clock skew)
- `ApiClient` — `GetAsync` / `PostAsync` with automatic Bearer header
- `ServiceCollectionExtensions.AddJwtAppToAppClient()` — one-line DI registration
- `AuthorizedHandler` — DelegatingHandler that forwards incoming Bearer tokens downstream
