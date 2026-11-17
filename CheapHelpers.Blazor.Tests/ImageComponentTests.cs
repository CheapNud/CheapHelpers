using Azure.Storage.Blobs;
using Bunit;
using CheapHelpers.Blazor.Shared;
using CheapHelpers.Services.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;

namespace CheapHelpers.Blazor.Tests;

// The dialog provider is rendered as its own root; bUnit shares the services, so it catches the dialog the component opens.
public class ImageComponentTests : BunitContext
{
    private const string ImageUrl = "https://example.test/photo.jpg";

    public ImageComponentTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton(new BlobService(new BlobServiceClient("UseDevelopmentStorage=true")));
    }

    private (IRenderedComponent<MudDialogProvider> Provider, IRenderedComponent<ImageComponent> Component) RenderImage(EventCallback<MouseEventArgs>? onClick = null)
    {
        var provider = Render<MudDialogProvider>();
        var cut = Render<ImageComponent>(parameters =>
        {
            parameters.Add(p => p.Uri, new Uri(ImageUrl));
            if (onClick is { } callback)
            {
                parameters.Add(p => p.OnClick, callback);
            }
        });
        return (provider, cut);
    }

    [Fact]
    public void Opens_the_image_full_screen_on_click()
    {
        var (provider, cut) = RenderImage();

        cut.Find("img").Click();

        Assert.Equal(ImageUrl, provider.FindComponent<Image_Dialog>().Instance.SourceUrl);
        Assert.Equal(ImageUrl, provider.Find("img").GetAttribute("src"));
        // MudBlazor exposes no public reader for a dialog's options; the container's parameter is the only place they live.
#pragma warning disable MUD0012
        var options = provider.FindComponent<MudDialogContainer>().Instance.Options;
#pragma warning restore MUD0012
        Assert.True(options.FullScreen);
        Assert.True(options.CloseOnEscapeKey);

        provider.Find("img").Click();

        provider.WaitForState(() => provider.FindComponents<Image_Dialog>().Count == 0);
    }

    [Fact]
    public void Keeps_a_custom_OnClick()
    {
        var clicked = false;
        var (provider, cut) = RenderImage(EventCallback.Factory.Create<MouseEventArgs>(this, () => clicked = true));

        cut.Find("img").Click();

        Assert.True(clicked);
        Assert.Empty(provider.FindComponents<Image_Dialog>());
    }
}
