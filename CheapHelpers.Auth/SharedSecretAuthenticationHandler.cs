using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace CheapHelpers.Auth;

/// <summary>
/// Authenticates machine-to-machine callers that send one fixed secret, in a header or as a bearer token.
/// The secret is compared in constant time.
/// </summary>
public class SharedSecretAuthenticationHandler(
    IOptionsMonitor<SharedSecretOptions> options,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder)
    : AuthenticationHandler<SharedSecretOptions>(options, loggerFactory, encoder)
{
    private const string BearerPrefix = "Bearer ";

    /// <inheritdoc />
    protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
        // The override has to return a Task and reading a header has nothing to await
        Task.FromResult(Authenticate());

    private AuthenticateResult Authenticate()
    {
        var providedSecret = ReadProvidedSecret();
        if (providedSecret is null)
            return AuthenticateResult.NoResult();

        if (!SecretMatches(providedSecret, Options.Secret))
            return AuthenticateResult.Fail("Invalid shared secret");

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, Options.PrincipalName)], Scheme.Name);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }

    private string? ReadProvidedSecret()
    {
        var headerSecret = Request.Headers[Options.HeaderName].ToString();
        if (headerSecret.Length > 0)
            return headerSecret;

        if (!Options.AllowBearer)
            return null;

        var authorization = Request.Headers[HeaderNames.Authorization].ToString();
        return authorization.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase)
            ? authorization[BearerPrefix.Length..].Trim()
            : null;
    }

    private static bool SecretMatches(string providedSecret, string expectedSecret)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expectedSecret);
        var providedBytes = Encoding.UTF8.GetBytes(providedSecret);
        var lengthMatches = providedBytes.Length == expectedBytes.Length;

        // On a length mismatch still compare a same-length buffer, so the time taken does not reveal
        // whether the length was right; the non-short-circuit & keeps the length check from deciding first
        var comparableBytes = lengthMatches ? providedBytes : new byte[expectedBytes.Length];
        return CryptographicOperations.FixedTimeEquals(comparableBytes, expectedBytes) & lengthMatches;
    }
}
