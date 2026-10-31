# CheapHelpers.Auth Documentation

Authentication building blocks for ASP.NET Core: Plex and OAuth sign-in, API keys, shared-secret auth, forwarded headers and Data Protection setup.

## Package Information

- **Package**: CheapHelpers.Auth
- **Target Framework**: .NET 11.0 (net11.0)
- **NuGet**: [CheapHelpers.Auth](https://www.nuget.org/packages/CheapHelpers.Auth)

CheapHelpers.Blazor references this package, so a Blazor app gets it automatically. CheapHelpers.Auth does not depend on CheapHelpers.Services, so an app that only needs Plex or OAuth sign-in without the Blazor components can reference CheapHelpers.Auth alone; CheapHelpers.Blazor still brings Services in.

## Namespaces

| Namespace | Contains |
|-----------|----------|
| `CheapHelpers.Auth` | Every `AddXxx`, `MapXxx` and `UseXxx` extension, `IExternalAuthProvider`, `IExternalUserProvisioner`, `ExternalUserInfo`, `ExternalProvisionResult`, the shared-secret handler |
| `CheapHelpers.Auth.Plex` | `IPlexAuthService`, `PlexAuthService`, `PlexAuthOptions`, `PlexUser`, `PlexPin`, `PlexAuthResult`, `PlexConstants` |
| `CheapHelpers.Auth.OAuth` | `OAuthProviderOptions` and the Google, Microsoft, GitHub, Apple and OpenID Connect options |
| `CheapHelpers.Auth.Testing` | `AddTestAuthentication`, `AsUser` and the test authentication handler |
| `CheapHelpers.Auth.ApiKeys` | `IApiKeyService`, `ApiKeyService<TUser>`, `ApiKeyOptions`, `ApiKeyMiddleware`, `IApiKeyValidator<TEntity>` and the result records |
| `CheapHelpers.Auth.Ota` | `AddCheapOta`, `MapCheapOtaReleaseEndpoints` and `OtaUpdateChecker` for gateway updates |

## Plex

Plex sign-in uses the plex.tv PIN flow and signs the user into the cookie scheme.

```csharp
using CheapHelpers.Auth;
using CheapHelpers.Auth.Plex;

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
builder.Services.AddPlexAuth(plexOptions =>
{
    plexOptions.ProductName = "MyApp";
    plexOptions.ClientIdentifier = "myapp-prod";
    plexOptions.AdminToken = builder.Configuration["Plex:AdminToken"]; // only for server-gated access
    plexOptions.PinPollAttempts = 5;
    plexOptions.AuthorizeUser = (plexUser, services, ct) =>
        services.GetRequiredService<IPlexAuthService>().HasServerAccessAsync(plexUser.Id, ct);
});

app.MapPlexAuthEndpoints(); // /auth/plex-start, /auth/plex-callback, /auth/logout
```

`CallbackBaseUrl` overrides the URL Plex sends the user back to. When it is null or blank the endpoint builds it from the request, so behind a reverse proxy either set it or configure forwarded headers (below).

CheapHelpers.Blazor has a `PlexSignInCard` component that links to the start endpoint.

## OAuth providers

Each call registers the provider's handler, a temporary external cookie and an `IExternalAuthProvider`. `MapOAuthEndpoints` maps the start and callback paths of every registered provider.

```csharp
builder.Services.AddGoogleAuth(google => { google.ClientId = "..."; google.ClientSecret = "..."; google.HostedDomain = "example.com"; });
builder.Services.AddMicrosoftAuth(microsoft => { microsoft.ClientId = "..."; microsoft.ClientSecret = "..."; microsoft.TenantId = "common"; });
builder.Services.AddGitHubAuth(github => { github.ClientId = "..."; github.ClientSecret = "..."; });
builder.Services.AddAppleAuth(apple =>
{
    apple.ClientId = "com.example.web";
    apple.TeamId = "...";
    apple.KeyId = "...";
    apple.PrivateKeyPath = "AuthKey.p8";
});

app.MapOAuthEndpoints(); // /auth/google-start, /auth/google-callback, ...
```

`HostedDomain` accepts only accounts whose Google `hd` claim matches the domain and whose email is at that domain.

Apple returns the user's name and email only on the first sign-in, so store them through `IExternalUserProvisioner` then. CheapHelpers.Blazor registers a provisioner that creates or links a `CheapUser` with `AddExternalUserProvisioning<TUser>()`.

## OpenID Connect

Any standards-compliant identity provider works (Authelia, Keycloak, Authentik). Endpoints come from the discovery document, the flow is authorization code with PKCE, and groups become role claims.

```csharp
builder.Services.AddOpenIdConnectAuth(oidc =>
{
    oidc.DisplayName = "Authelia";
    oidc.Authority = "https://auth.example.com";
    oidc.ClientId = "myapp";
    oidc.ClientSecret = builder.Configuration["Oidc:ClientSecret"]!;
});

app.MapOAuthEndpoints(); // /auth/oidc-start, /auth/oidc-callback; the IdP redirects to /signin-oidc
```

To run two OIDC providers side by side, give each its own `AuthenticationScheme`, `RemoteCallbackPath`, `StartPath` and `CallbackPath`.

## API keys and middleware

Keys are stored as SHA-256 hashes in the `ApiKeys` table of `CheapBusinessContext<TUser>`; the full key is only returned when it is created.

```csharp
builder.Services.AddCheapApiKeys<MyUser>(apiKeyOptions =>
{
    apiKeyOptions.KeyPrefix = "myapp_";
    apiKeyOptions.DefaultRateLimitPerMinute = 100;
    apiKeyOptions.ProtectedPaths = ["/api"];
});

app.UseAuthentication();
app.UseCheapApiKeyMiddleware(); // after UseAuthentication, before UseAuthorization
app.UseAuthorization();
```

```csharp
var created = await apiKeyService.GenerateAsync(userId, "Production API", scopes: ["read", "write"], cancellationToken: ct);
// created.FullKey is shown once; store it now

var validation = await apiKeyService.ValidateAsync(rawKey, ct);
```

The middleware reads the key from the `X-Api-Key` header, applies the per-key minute and day limits, and sets a principal with the owner's id and one `scope` claim per scope.

## Shared secret

For machine-to-machine endpoints (webhooks, health checks, metrics) that share one secret from configuration. The secret is compared in constant time.

```csharp
builder.Services.AddAuthentication()
    .AddSharedSecret(sharedSecret => sharedSecret.Secret = builder.Configuration["Webhook:Secret"]!);

app.MapPost("/webhooks/scan", HandleScan)
    .RequireAuthorization(policy => policy
        .AddAuthenticationSchemes(SharedSecretDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser());
```

Callers send `X-Shared-Secret: <secret>` or `Authorization: Bearer <secret>`. `HeaderName`, `AllowBearer` and `PrincipalName` change the defaults. An empty `Secret` throws on first use.

## Test authentication

A header-driven authentication scheme for integration tests, in the `CheapHelpers.Auth.Testing` namespace. It signs a request in as whoever the `Authorization: Test <userId>[;key=value]*` header names. The user id becomes the `NameIdentifier` and `sub` claims, every `key=value` pair becomes a claim. No header gives no result, so an endpoint that requires a user answers 401. It authenticates only in the Development or Test environment; anywhere else, or with no environment registered, every request fails, so a registration left in by mistake cannot sign anyone in.

```csharp
using CheapHelpers.Auth.Testing;

builder.Services.AddTestAuthentication();
builder.Services.AddAuthorization();
// later, on the client
var client = app.GetTestClient().AsUser("u1", ("role", "Admin"));
```

## Gateway updates

The `CheapHelpers.Auth.Ota` namespace is the server half of over-the-air updates for gateway agents. It reads releases from a source, decides with `OtaUpdatePolicy` which release a device should install, and maps the public release endpoints that agents, installers and other instances call. Enrollment, device keys, heartbeats and the device-authenticated update check stay in the app.

The source is picked with `Source`. Every source needs `Assets`, which maps an architecture key to its binary asset name.

| `Source` | Reads from | Setting |
|----------|------------|---------|
| `Forge` (default) | The release API of a Gitea or Forgejo repository; the list is cached for `ReleaseCacheDuration` (5 minutes) | `ForgeRepoApiUrl`, optional `ForgeToken` |
| `Upstream` | The release endpoints of another instance | `UpstreamUrl`, for example `https://other.example/api/ota` |
| `LocalFolder` | A folder laid out as `{version}/{asset}`, with an empty `mandatory` file to mark a release | `LocalPath` |

`InstancePin` keeps every device without its own pin on one version. A missing setting throws at startup with its name.

```json
{
  "Ota": {
    "Source": "Forge",
    "ForgeRepoApiUrl": "https://forge.example/api/v1/repos/owner/agent",
    "ForgeToken": "<from a secret store>",
    "Assets": { "armv7": "agent-armv7", "aarch64": "agent-aarch64" }
  }
}
```

```csharp
using CheapHelpers.Auth.Ota;

builder.Services.AddCheapOta(builder.Configuration.GetSection("Ota"));
builder.Services.AddRateLimiter(/* define a "public" policy */);

app.MapCheapOtaReleaseEndpoints().RequireRateLimiting("public"); // /api/ota/...
```

The app's own device endpoint authenticates the device, loads its pin and asks `OtaUpdateChecker`:

```csharp
app.MapGet("/api/devices/update", async (HttpContext http, OtaUpdateChecker checker, CancellationToken ct) =>
{
    var device = await LoadAuthenticatedDeviceAsync(http, ct); // the app's own code
    var update = await checker.CheckAsync(device.Version, device.Architecture, device.Pin, "/api/ota", ct);
    return update is null ? Results.NoContent() : Results.Ok(update);
});
```

The two paths in the answer are relative to the host, so pass the path base in `routePrefix` when the app runs under one. The `latest` endpoint takes its prefix from the request, which includes a path base or a parent group.

### Wire contract

| Endpoint | Query | Status codes |
|----------|-------|--------------|
| `GET /api/ota/releases` | none | 200, 503 |
| `GET /api/ota/releases/latest` | `arch` (required), `since` (optional current version) | 200, 400 unknown or missing `arch`, 404 no release, 503 |
| `GET /api/ota/releases/{version}/{assetName}` | none | 200 `application/octet-stream`, 400 unknown asset name, 404 missing, 503 |

A 503 carries the plain text `Release source unavailable`. All three are anonymous; rate limiting is the app's choice.

An update answer (`OtaUpdateCheck`), from `latest` or from `CheckAsync`:

```json
{
  "version": "1.4.2",
  "tag": "v1.4.2",
  "mandatory": false,
  "download_url": "/api/ota/releases/1.4.2/agent-armv7",
  "sha256_url": "/api/ota/releases/1.4.2/agent-armv7.sha256"
}
```

`mandatory` is true when any release above the device's current version is mandatory. The release list is newest first:

```json
[ { "version": "1.4.2", "tag": "v1.4.2", "mandatory": false } ]
```

Releases are tagged `<TagPrefix><major.minor.patch>` (`v1.4.2` by default); tags without the prefix or with a version that does not parse are ignored. A release is mandatory when its body contains `[mandatory]` (`MandatoryMarker`, any case). A mandatory release also lifts a pinned device. Each binary has a sibling asset `<asset>.sha256` that CI writes with `sha256sum` (`<64 hex>  <asset name>` and a newline); the server passes it through untouched. Devices download through these endpoints and never talk to the forge.

## Forwarded headers

Trusts `X-Forwarded-For` and `X-Forwarded-Proto` only from the proxies you list. Each entry is an IP address or a CIDR network, and must be the address the proxy connects from (for a container, its address on the shared network).

```json
{
  "Proxy": {
    "KnownProxies": [ "172.18.0.2", "10.0.0.0/24" ],
    "AllowedHosts": [ "app.example.com" ]
  }
}
```

`AllowedHosts` is optional. Only when it has entries is `X-Forwarded-Host` trusted, and only for those hosts, because many proxies pass a client-supplied `X-Forwarded-Host` through, which lets a caller poison the host used in links such as the Plex callback or password-reset emails.

```csharp
builder.Services.AddCheapForwardedHeaders(builder.Configuration);

var app = builder.Build();
app.UseForwardedHeaders(); // first, before anything that reads the client IP or scheme
```

The framework's loopback defaults are cleared, so only the listed proxies are trusted. With no entries nothing is configured and a warning is logged at startup. An entry that is not an IP address or network throws at startup.

## Data Protection

Persists the Data Protection key ring so auth cookies and antiforgery tokens survive a restart or redeploy.

```json
{
  "DataProtection": {
    "KeysPath": "/data/keys"
  }
}
```

```csharp
builder.Services.AddCheapDataProtection(builder.Configuration, "MyApp");
```

Without `KeysPath` the keys go to `{LocalApplicationData}/{applicationName}/keys` (for example `~/.local/share/MyApp/keys` on Linux), so a redeploy that replaces the app folder keeps the keys. Only when that location does not resolve, as in a container without `HOME`, do they fall back to a `keys` folder next to the app; set `KeysPath` there. A folder this call creates is readable by the app's user only (mode 700 on Linux and macOS); an existing folder keeps its permissions and a warning is logged when other users can read it. The path is logged at startup. In a container, set `KeysPath` to a mounted volume, or every restart signs everyone out. With the default path on Windows the key files are encrypted with DPAPI for the account the app runs as. With a configured `KeysPath` the files are written as plain XML so a key ring shared between nodes keeps working; the call returns the `IDataProtectionBuilder`, so add `.ProtectKeysWithCertificate(cert)` or `.ProtectKeysWithDpapi()` when that folder needs encryption at rest, and keep it off mounts and backups that others can read.
