using Bunit;
using CheapHelpers.Blazor.Shared;
using MudBlazor;
using MudBlazor.Services;

namespace CheapHelpers.Blazor.Tests;

public class FileDisplayTests : BunitContext
{
    private const string FileUrl = "https://example.test/files/sample";
    private const string SignedPdfUrl = "https://example.test/files/scan.pdf?sig=abc";

    public FileDisplayTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
    }

    private IRenderedComponent<FileDisplay> RenderFile(string url, string? contentType, string? title = null) =>
        Render<FileDisplay>(parameters => parameters
            .Add(p => p.Url, url)
            .Add(p => p.ContentType, contentType)
            .Add(p => p.Title, title));

    [Fact]
    public void Renders_an_image_for_an_image_type()
    {
        var cut = RenderFile(FileUrl, "image/png");

        Assert.Single(cut.FindComponents<MudImage>());
        Assert.Equal(FileUrl, cut.Find("img").GetAttribute("src"));
        Assert.Empty(cut.FindAll("iframe"));
    }

    [Fact]
    public void Renders_a_video_with_a_typed_source_for_a_video_type()
    {
        var cut = RenderFile(FileUrl, "video/mp4");

        var source = cut.Find("video source");
        Assert.Equal(FileUrl, source.GetAttribute("src"));
        Assert.Equal("video/mp4", source.GetAttribute("type"));
    }

    [Fact]
    public void Announces_a_quicktime_video_as_mp4()
    {
        var cut = RenderFile("https://example.test/files/clip.mov", null);

        Assert.Equal("video/mp4", cut.Find("video source").GetAttribute("type"));
    }

    [Fact]
    public void Renders_a_frame_for_a_pdf()
    {
        var cut = RenderFile(FileUrl, "application/pdf");

        Assert.Equal(FileUrl, cut.Find("iframe").GetAttribute("src"));
    }

    [Fact]
    public void Renders_only_the_open_link_for_other_types()
    {
        var cut = RenderFile(FileUrl, "application/zip");

        Assert.Empty(cut.FindAll("img"));
        Assert.Empty(cut.FindAll("video"));
        Assert.Empty(cut.FindAll("iframe"));
        AssertOpenLink(cut, FileUrl);
    }

    [Fact]
    public void Falls_back_to_the_url_extension_without_the_query_string()
    {
        var cut = RenderFile(SignedPdfUrl, null);

        Assert.Equal(SignedPdfUrl, cut.Find("iframe").GetAttribute("src"));
    }

    [Theory]
    [InlineData("image/png")]
    [InlineData("video/mp4")]
    [InlineData("application/pdf")]
    [InlineData("application/zip")]
    public void Every_kind_has_an_open_link_in_a_new_tab(string contentType)
    {
        var cut = RenderFile(FileUrl, contentType);

        AssertOpenLink(cut, FileUrl);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData(" JavaScript:alert(1)")]
    [InlineData("data:application/pdf;base64,AAAA")]
    public void Renders_no_link_or_frame_for_a_script_url(string url)
    {
        var cut = RenderFile(url, "application/pdf");

        Assert.Empty(cut.FindAll("a"));
        Assert.Empty(cut.FindAll("iframe"));
    }

    [Fact]
    public void Keeps_a_relative_url()
    {
        var cut = RenderFile("files/scan.pdf", null);

        Assert.Equal("files/scan.pdf", cut.Find("iframe").GetAttribute("src"));
    }

    [Fact]
    public void Renders_the_title_as_text_not_markup()
    {
        const string MarkupTitle = "<b>x</b>";

        var cut = RenderFile(FileUrl, "application/zip", MarkupTitle);

        Assert.Empty(cut.FindAll("b"));
        Assert.Contains(MarkupTitle, cut.Find(".mud-typography").TextContent);
    }

    private static void AssertOpenLink(IRenderedComponent<FileDisplay> cut, string url) =>
        Assert.Contains(cut.FindAll("a"), link => link.GetAttribute("href") == url && link.GetAttribute("target") == "_blank");
}
