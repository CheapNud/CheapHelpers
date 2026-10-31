using System.Net;
using CheapHelpers.Networking.Core;
using CheapHelpers.Networking.Detection;
using CheapHelpers.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CheapHelpers.Services.Tests;

public class UpnpDetectorTests
{
    private const string DeviceAddress = "192.168.1.20";
    private const string DescriptionUrl = "http://192.168.1.20:49152/desc.xml";
    private const string ServerHeader = "TestServer/1.0";
    private const string FallbackInfo = "TestServer/1.0 (UPnP)";
    private const string XmlContentType = "text/xml";

    private const string DescriptionXml = """
        <root xmlns="urn:schemas-upnp-org:device-1-0">
          <device>
            <deviceType>urn:schemas-upnp-org:device:MediaServer:1</deviceType>
            <friendlyName>Living Room</friendlyName>
            <manufacturer>Acme</manufacturer>
          </device>
        </root>
        """;

    private const string ExternalEntityXml = """
        <?xml version="1.0"?>
        <!DOCTYPE root [<!ENTITY secret SYSTEM "file:///c:/windows/win.ini">]>
        <root xmlns="urn:schemas-upnp-org:device-1-0">
          <device>
            <friendlyName>&secret;</friendlyName>
          </device>
        </root>
        """;

    [Theory]
    [InlineData(DescriptionUrl, DeviceAddress)]
    [InlineData(DescriptionUrl, "::ffff:192.168.1.20")]
    public void IsAcceptableLocation_OwnAddressOverHttp_Accepts(string location, string sender)
    {
        Assert.True(UpnpDetector.IsAcceptableLocation(location, IPAddress.Parse(sender), out var uri));
        Assert.Equal(new Uri(location), uri);
    }

    [Theory]
    [InlineData("http://192.168.1.99:49152/desc.xml", DeviceAddress)]
    [InlineData("http://router.local:49152/desc.xml", DeviceAddress)]
    [InlineData("https://192.168.1.20:49152/desc.xml", DeviceAddress)]
    [InlineData("http://127.0.0.1:49152/desc.xml", "127.0.0.1")]
    [InlineData("http://169.254.10.10:49152/desc.xml", "169.254.10.10")]
    [InlineData("http://0.0.0.0:49152/desc.xml", "0.0.0.0")]
    [InlineData("ftp://192.168.1.20/desc.xml", DeviceAddress)]
    [InlineData("/desc.xml", DeviceAddress)]
    public void IsAcceptableLocation_OtherHostOrUnsafeAddress_Rejects(string location, string sender)
    {
        Assert.False(UpnpDetector.IsAcceptableLocation(location, IPAddress.Parse(sender), out var uri));
        Assert.Null(uri);
    }

    [Fact]
    public async Task ProcessSsdpResponse_LocationOnOtherHost_SendsNoRequest()
    {
        var handler = new RecordingHttpHandler();
        using var detector = CreateDetector(handler);

        var info = await detector.ProcessSsdpResponseAsync(SsdpReply("http://192.168.1.99:49152/desc.xml"), IPAddress.Parse(DeviceAddress));

        Assert.Null(info);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task ProcessSsdpResponse_LocationOnSender_FetchesThatUrlOnce()
    {
        var handler = new RecordingHttpHandler().Reply(DescriptionXml, contentType: XmlContentType);
        using var detector = CreateDetector(handler);

        var info = await detector.ProcessSsdpResponseAsync(SsdpReply(DescriptionUrl), IPAddress.Parse(DeviceAddress));

        Assert.Equal("Living Room - Media Server (UPnP)", info);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(new Uri(DescriptionUrl), request.RequestUri);
    }

    [Fact]
    public async Task ProcessSsdpResponse_ExternalEntity_FallsBackToServerHeader()
    {
        var handler = new RecordingHttpHandler().Reply(ExternalEntityXml, contentType: XmlContentType);
        using var detector = CreateDetector(handler);

        var info = await detector.ProcessSsdpResponseAsync(SsdpReply(DescriptionUrl), IPAddress.Parse(DeviceAddress));

        Assert.Equal(FallbackInfo, info);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task ProcessSsdpResponse_Redirect_IsNotFollowed()
    {
        var handler = new RecordingHttpHandler().Respond(_ => new HttpResponseMessage(HttpStatusCode.Found)
        {
            Headers = { Location = new Uri("http://127.0.0.1/desc.xml") }
        });
        using var detector = CreateDetector(handler);

        var info = await detector.ProcessSsdpResponseAsync(SsdpReply(DescriptionUrl), IPAddress.Parse(DeviceAddress));

        Assert.Equal(FallbackInfo, info);
        Assert.Single(handler.Requests);
    }

    private static UpnpDetector CreateDetector(RecordingHttpHandler handler) =>
        new(NullLogger<UpnpDetector>.Instance, Options.Create(new PortDetectionOptions()), handler.CreateClient());

    private static string SsdpReply(string location) =>
        $"HTTP/1.1 200 OK\r\nLOCATION: {location}\r\nSERVER: {ServerHeader}\r\n\r\n";
}
