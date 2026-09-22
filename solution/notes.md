# Solution notes
- Library targets net8.0; bump TargetFramework if your consumers use net7/net9.
- Secrets live in appsettings.json for demo only; use user-secrets / env vars / Key Vault in production.
- TokenProvider uses a SemaphoreSlim so concurrent callers share a single token request (singleflight).
