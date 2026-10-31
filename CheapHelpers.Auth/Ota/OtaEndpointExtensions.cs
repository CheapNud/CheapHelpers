using CheapHelpers.Ota;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace CheapHelpers.Auth.Ota;

/// <summary>
/// Maps the public release endpoints that gateway agents, installers and other instances call.
/// </summary>
public static class OtaEndpointExtensions
{
    private const string DefaultRoutePrefix = "/api/ota";
    private const string AssetContentType = "application/octet-stream";
    private const string UnavailableMessage = "Release source unavailable";
    private const string TextContentType = "text/plain";
    private const string LatestPath = "/releases/latest";

    /// <summary>
    /// Maps <c>GET {prefix}/releases</c>, <c>GET {prefix}/releases/latest?arch=&amp;since=</c> and
    /// <c>GET {prefix}/releases/{version}/{assetName}</c>, all anonymous. Add rate limiting to the returned group.
    /// </summary>
    /// <remarks>
    /// The list answers <c>[{version, tag, mandatory}]</c> newest first, latest answers an <see cref="OtaUpdateCheck"/> (404 without releases, 400 for an unknown architecture), an asset streams as octet-stream (400 for an unknown name, 404 when missing), and a source that cannot be reached gives 503.
    /// Releases are tagged <c>{TagPrefix}{version}</c> and marked mandatory by <see cref="OtaOptions.MandatoryMarker"/> in the body; the <c>.sha256</c> asset is CI-produced <c>sha256sum</c> output passed through untouched.
    /// </remarks>
    public static RouteGroupBuilder MapCheapOtaReleaseEndpoints(this IEndpointRouteBuilder endpoints, string routePrefix = DefaultRoutePrefix)
    {
        var group = endpoints.MapGroup(routePrefix).AllowAnonymous();

        // The asset endpoint opens its stream before it returns, so a source failure is caught here before any byte is sent.
        // HttpClient reports its own timeout as a cancellation with a TimeoutException inside; a client abort is left alone.
        // Local disk errors stay 500, because they are a fault of this server rather than an unreachable source.
        group.AddEndpointFilter(async (invocationContext, next) =>
        {
            try
            {
                return await next(invocationContext);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException { InnerException: TimeoutException })
            {
                return Results.Text(UnavailableMessage, TextContentType, statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        });

        group.MapGet("/releases", async (IOtaReleaseSource source, CancellationToken cancellationToken) =>
            Results.Ok(await source.GetReleasesAsync(cancellationToken)));

        group.MapGet(LatestPath, async (string? arch, string? since, OtaUpdateChecker checker, HttpContext httpContext) =>
        {
            if (string.IsNullOrWhiteSpace(arch))
                return Results.BadRequest();

            // The links carry the prefix this request came in on, so a path base or a parent group is included
            var requestPath = (httpContext.Request.PathBase + httpContext.Request.Path).Value!.TrimEnd('/');
            var mountedPrefix = requestPath[..^LatestPath.Length];

            try
            {
                var latest = await checker.LatestAsync(since, arch, mountedPrefix, httpContext.RequestAborted);
                return latest is null ? Results.NotFound() : Results.Ok(latest);
            }
            catch (ArgumentException)
            {
                return Results.BadRequest();
            }
        });

        group.MapGet("/releases/{version}/{assetName}", async (
            string version,
            string assetName,
            IOtaReleaseSource source,
            IOptions<OtaOptions> options,
            CancellationToken cancellationToken) =>
        {
            if (!options.Value.IsKnownAsset(assetName))
                return Results.BadRequest();

            var assetStream = await source.OpenAssetAsync(version, assetName, cancellationToken);
            return assetStream is null
                ? Results.NotFound()
                : Results.Stream(assetStream, AssetContentType, fileDownloadName: assetName);
        });

        return group;
    }
}
