using Azure.Storage.Blobs;
using Bunit;
using CheapHelpers.Blazor.Shared;
using CheapHelpers.EF;
using CheapHelpers.Models.Entities;
using CheapHelpers.Services.Storage;
using CheapHelpers.Services.Storage.Configuration;
using CheapHelpers.Services.Vision;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using MudBlazor;
using MudBlazor.Services;

namespace CheapHelpers.Blazor.Tests;

// The Analyze click itself is not exercised: it downloads the image from blob storage, which these tests never reach.
// The BlobService only signs URLs locally here, against the development storage account.
public class ImagePanelTests : BunitContext
{
    private const string AnalyzeButton = "button[aria-label='Analyze']";
    private const string Description = "a dog on a beach";

    public ImagePanelTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddLocalization();
        Services.AddScoped<IStringLocalizer>(sp => sp.GetRequiredService<IStringLocalizerFactory>().Create(typeof(Properties.Language)));
        Services.AddTestIdentity();
        var databaseName = Guid.NewGuid().ToString();
        Services.AddDbContextFactory<CheapCommunicationContext<TestUser>>(options => options.UseInMemoryDatabase(databaseName));
        Services.AddSingleton(new BlobService(new BlobServiceClient("UseDevelopmentStorage=true")));
    }

    [Fact]
    public void Hides_the_analyze_button_when_analysis_is_off()
    {
        Services.AddSingleton<IVisionService, FakeVisionService>();

        var cut = RenderPanel(enableAnalysis: false, new GenericFileAttachment { FileName = "photo.jpg" });

        Assert.Empty(cut.FindAll(AnalyzeButton));
    }

    [Fact]
    public void Hides_the_analyze_button_without_a_vision_service()
    {
        var cut = RenderPanel(enableAnalysis: true, new GenericFileAttachment { FileName = "photo.jpg" });

        Assert.Empty(cut.FindAll(AnalyzeButton));
    }

    [Fact]
    public void Shows_the_analyze_button_when_analysis_is_on_and_a_service_is_registered()
    {
        Services.AddSingleton<IVisionService, FakeVisionService>();

        var cut = RenderPanel(enableAnalysis: true, new GenericFileAttachment { FileName = "photo.jpg" });

        Assert.Single(cut.FindAll(AnalyzeButton));
    }

    [Fact]
    public void Shows_the_stored_description_and_tags()
    {
        var image = new GenericFileAttachment { FileName = "photo.jpg", Description = Description };
        image.SetTags(["dog", "beach"]);

        var cut = RenderPanel(enableAnalysis: false, image);

        Assert.Contains(cut.FindAll(".mud-typography-body2"), text => text.TextContent.Trim() == Description);
        Assert.Equal(["dog", "beach"], cut.FindComponents<MudChip<string>>().Select(c => c.Instance.Text));
    }

    private IRenderedComponent<ImagePanel<TestUser>> RenderPanel(bool enableAnalysis, FileAttachment image) =>
        Render<ImagePanel<TestUser>>(parameters => parameters
            .Add(p => p.Files, [image])
            .Add(p => p.BlobContainer, BlobContainers.TempContainer)
            .Add(p => p.CanDelete, false)
            .Add(p => p.EnableImageAnalysis, enableAnalysis));
}
