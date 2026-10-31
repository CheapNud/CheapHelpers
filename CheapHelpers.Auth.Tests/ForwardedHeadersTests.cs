using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;

namespace CheapHelpers.Auth.Tests;

public class ForwardedHeadersTests
{
    private const string ProxyAddress = "10.0.0.5";
    private const string ClientAddress = "203.0.113.7";
    private const string ForwardedHost = "app.example.com";
    private const string RequestHost = "localhost";

    [Fact]
    public async Task A_known_proxy_rewrites_the_remote_address()
    {
        var remoteAddress = await GetRemoteAddressAsync(new() { ["Proxy:KnownProxies:0"] = ProxyAddress });

        Assert.Equal(ClientAddress, remoteAddress);
    }

    [Fact]
    public async Task A_known_network_rewrites_the_remote_address()
    {
        var remoteAddress = await GetRemoteAddressAsync(new() { ["Proxy:KnownProxies:0"] = "10.0.0.0/24" });

        Assert.Equal(ClientAddress, remoteAddress);
    }

    [Fact]
    public async Task No_configuration_leaves_the_remote_address_untouched()
    {
        var remoteAddress = await GetRemoteAddressAsync([]);

        Assert.Equal(ProxyAddress, remoteAddress);
    }

    [Theory]
    [InlineData(null, RequestHost)]
    [InlineData(ForwardedHost, ForwardedHost)]
    public async Task X_forwarded_host_is_applied_only_for_configured_allowed_hosts(string? allowedHost, string expectedHost)
    {
        Dictionary<string, string?> settings = new() { ["Proxy:KnownProxies:0"] = ProxyAddress };
        if (allowedHost is not null)
            settings["Proxy:AllowedHosts:0"] = allowedHost;

        var httpContext = await SendThroughProxyAsync(settings);

        Assert.Equal(expectedHost, httpContext.Request.Host.Value);
    }

    private static async Task<string?> GetRemoteAddressAsync(Dictionary<string, string?> settings)
    {
        var httpContext = await SendThroughProxyAsync(settings);
        return httpContext.Connection.RemoteIpAddress?.ToString();
    }

    private static async Task<HttpContext> SendThroughProxyAsync(Dictionary<string, string?> settings)
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var app = await TestWebApp.StartAsync(
            builder =>
            {
                builder.Configuration.AddInMemoryCollection(settings);
                builder.Services.AddCheapForwardedHeaders(builder.Configuration);
            },
            webApp => webApp.UseForwardedHeaders(),
            cancellationToken);

        return await app.GetTestServer().SendAsync(requestContext =>
        {
            requestContext.Connection.RemoteIpAddress = IPAddress.Parse(ProxyAddress);
            requestContext.Request.Host = new HostString(RequestHost);
            requestContext.Request.Headers["X-Forwarded-For"] = ClientAddress;
            requestContext.Request.Headers["X-Forwarded-Host"] = ForwardedHost;
        }, cancellationToken);
    }
}
