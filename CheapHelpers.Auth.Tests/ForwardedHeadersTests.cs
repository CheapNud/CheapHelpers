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

    private static async Task<string?> GetRemoteAddressAsync(Dictionary<string, string?> settings)
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var app = await TestApp.StartAsync(
            builder =>
            {
                builder.Configuration.AddInMemoryCollection(settings);
                builder.Services.AddCheapForwardedHeaders(builder.Configuration);
            },
            webApp => webApp.UseForwardedHeaders(),
            cancellationToken);

        var httpContext = await app.GetTestServer().SendAsync(requestContext =>
        {
            requestContext.Connection.RemoteIpAddress = IPAddress.Parse(ProxyAddress);
            requestContext.Request.Headers["X-Forwarded-For"] = ClientAddress;
        }, cancellationToken);

        return httpContext.Connection.RemoteIpAddress?.ToString();
    }
}
