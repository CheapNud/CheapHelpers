using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CheapHelpers.Auth.Testing;

/// <summary>
/// Signs a request in as whoever its Authorization header names, so tests can pick the user per request.
/// The header reads <c>Authorization: Test &lt;userId&gt;[;key=value]*</c>, for example <c>Test u1;role=Admin;email=a@b.c</c>.
/// The user id becomes the <see cref="ClaimTypes.NameIdentifier"/> claim and the <c>sub</c> claim, and every further
/// <c>key=value</c> pair becomes a claim of type <c>key</c>. Values cannot contain <c>;</c> or <c>=</c>.
/// No header, or a header with another scheme word, gives no result, so an endpoint that requires a user answers 401.
/// </summary>
public sealed class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    /// <summary>The scheme name, which is also the first word of the Authorization header.</summary>
    public const string SchemeName = "Test";

    /// <summary>The claim type that carries the user id next to <see cref="ClaimTypes.NameIdentifier"/>.</summary>
    public const string SubjectClaimType = "sub";

    private const string HeaderPrefix = SchemeName + " ";
    private const char SegmentSeparator = ';';
    private const char PairSeparator = '=';

    /// <inheritdoc />
#pragma warning disable CS1998 // the override has to return a Task, and reading a header has nothing to await
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync() =>
#pragma warning restore CS1998
        Authenticate(Request.Headers.Authorization.ToString());

    private static AuthenticateResult Authenticate(string header)
    {
        if (!header.StartsWith(HeaderPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var segments = header[HeaderPrefix.Length..].Split(SegmentSeparator, StringSplitOptions.TrimEntries);
        var userId = segments[0];
        if (userId.Length == 0)
        {
            return AuthenticateResult.Fail("The Test header names no user id.");
        }

        List<Claim> claims = [new(ClaimTypes.NameIdentifier, userId), new(SubjectClaimType, userId)];
        foreach (var segment in segments.Skip(1).Where(segment => segment.Length > 0))
        {
            var pair = segment.Split(PairSeparator, 2, StringSplitOptions.TrimEntries);
            if (pair.Length != 2 || pair[0].Length == 0)
            {
                return AuthenticateResult.Fail($"The Test header segment '{segment}' is not key=value.");
            }

            claims.Add(new Claim(pair[0], pair[1]));
        }

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
        return AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName));
    }
}
