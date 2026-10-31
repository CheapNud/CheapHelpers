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

A header-driven authentication scheme for integration tests, in the `CheapHelpers.Auth.Testing` namespace. It signs a request in as whoever the `Authorization: Test <userId>[;key=value]*` header names. The user id becomes the `NameIdentifier` and `sub` claims, every `key=value` pair becomes a claim. No header gives no result, so an endpoint that requires a user answers 401. In the Production environment the scheme fails every request, so a registration left in by mistake cannot sign anyone in.

```csharp
using CheapHelpers.Auth.Testing;

builder.Services.AddTestAuthentication();
builder.Services.AddAuthorization();
// later, on the client
var client = app.GetTestClient().AsUser("u1", ("role", "Admin"));
```

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
