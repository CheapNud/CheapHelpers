# CheapHelpers.Auth Documentation

Authentication building blocks for ASP.NET Core: Plex and OAuth sign-in, API keys, shared-secret auth, forwarded headers and Data Protection setup.

## Package Information

- **Package**: CheapHelpers.Auth
- **Version**: 3.9.0
- **Target Framework**: .NET 11.0 (net11.0)
- **NuGet**: [CheapHelpers.Auth](https://www.nuget.org/packages/CheapHelpers.Auth)

CheapHelpers.Blazor references this package, so a Blazor app gets it automatically. An app that only signs in through Plex or OAuth can reference CheapHelpers.Auth and CheapHelpers.Blazor without CheapHelpers.Services.

## Namespaces

| Namespace | Contains |
|-----------|----------|
| `CheapHelpers.Auth` | Every `AddXxx`, `MapXxx` and `UseXxx` extension, `IExternalAuthProvider`, `IExternalUserProvisioner`, `ExternalUserInfo`, `ExternalProvisionResult`, the shared-secret handler |
| `CheapHelpers.Auth.Plex` | `IPlexAuthService`, `PlexAuthService`, `PlexAuthOptions`, `PlexUser`, `PlexPin`, `PlexAuthResult`, `PlexConstants` |
| `CheapHelpers.Auth.OAuth` | `OAuthProviderOptions` and the Google, Microsoft, GitHub, Apple and OpenID Connect options |
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

## Forwarded headers

Trusts `X-Forwarded-For`, `X-Forwarded-Proto` and `X-Forwarded-Host` only from the proxies you list. Each entry is an IP address or a CIDR network, and must be the address the proxy connects from (for a container, its address on the shared network).

```json
{
  "Proxy": {
    "KnownProxies": [ "172.18.0.2", "10.0.0.0/24" ]
  }
}
```

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

Without `KeysPath` the keys go to a `keys` folder next to the app. The folder is created and its path is logged at startup. Mount it on a volume in a container, or every redeploy signs everyone out. The key files are not encrypted at rest, so keep the folder readable only by the app.
