using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CheapHelpers.Auth;

/// <summary>
/// Registers forwarded-header handling that trusts only the reverse proxies named in configuration.
/// </summary>
public static class ForwardedHeadersExtensions
{
    private const string KnownProxiesKey = "KnownProxies";
    private const char CidrSeparator = '/';

    /// <summary>
    /// Trusts <c>X-Forwarded-For</c>, <c>X-Forwarded-Proto</c> and <c>X-Forwarded-Host</c> only from the proxies
    /// listed under <c>{sectionKey}:KnownProxies</c>. Each entry is an IP address (<c>10.0.0.5</c>) or a CIDR
    /// network (<c>172.18.0.0/16</c>), and must be the address the proxy connects from.
    /// The framework's loopback defaults are cleared, so only listed proxies are trusted.
    /// When the list is empty nothing is configured and a warning is logged at startup.
    /// The app must still call <c>app.UseForwardedHeaders()</c> early in the pipeline.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Configuration holding the proxy section.</param>
    /// <param name="sectionKey">Section that holds the <c>KnownProxies</c> array.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="FormatException">An entry is not a valid IP address or CIDR network.</exception>
    public static IServiceCollection AddCheapForwardedHeaders(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionKey = "Proxy")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionKey);

        string[] proxyEntries = [.. configuration.GetSection(sectionKey).GetSection(KnownProxiesKey).GetChildren()
            .Select(proxyEntry => proxyEntry.Value?.Trim())
            .OfType<string>()
            .Where(proxyEntry => proxyEntry.Length > 0)];

        if (proxyEntries.Length == 0)
        {
            services.AddSingleton<IStartupFilter>(new StartupLogFilter(LogLevel.Warning,
                "No {Section}:KnownProxies configured, so forwarded headers are not trusted and every request behind a reverse proxy appears to come from the proxy",
                sectionKey));
            return services;
        }

        // Parse up front so a typo fails at startup instead of silently trusting nothing
        List<IPAddress> knownProxies = [];
        List<System.Net.IPNetwork> knownNetworks = [];
        foreach (var proxyEntry in proxyEntries)
        {
            if (proxyEntry.Contains(CidrSeparator))
                knownNetworks.Add(System.Net.IPNetwork.Parse(proxyEntry));
            else
                knownProxies.Add(IPAddress.Parse(proxyEntry));
        }

        services.Configure<ForwardedHeadersOptions>(forwardedOptions =>
        {
            forwardedOptions.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
            forwardedOptions.KnownProxies.Clear();
            forwardedOptions.KnownIPNetworks.Clear();

            foreach (var knownProxy in knownProxies)
                forwardedOptions.KnownProxies.Add(knownProxy);

            foreach (var knownNetwork in knownNetworks)
                forwardedOptions.KnownIPNetworks.Add(knownNetwork);
        });

        return services;
    }
}
