using CheapHelpers.Blazor.Helpers;
using CheapHelpers.Models.Entities;
using CheapHelpers.Services.Vision;

namespace CheapHelpers.Blazor.Tests;

public class ImageAnalysisApplierTests
{
    private const string Caption = "a dog on a beach";

    [Fact]
    public void Applies_the_caption_and_tags_in_confidence_order()
    {
        var attachment = new GenericFileAttachment();
        var result = Success(Caption, ("sand", 0.7), ("dog", 0.95), ("beach", 0.8));

        Assert.True(ImageAnalysisApplier.Apply(attachment, result));

        Assert.Equal(Caption, attachment.Description);
        Assert.Equal(["dog", "beach", "sand"], attachment.GetTags());
    }

    [Fact]
    public void Drops_tags_below_the_confidence_floor()
    {
        var attachment = new GenericFileAttachment();

        ImageAnalysisApplier.Apply(attachment, Success(Caption, ("dog", 0.9), ("cat", 0.49), ("ball", ImageAnalysisApplier.MinTagConfidence)));

        Assert.Equal(["dog", "ball"], attachment.GetTags());
    }

    [Fact]
    public void Keeps_at_most_the_tag_cap()
    {
        var attachment = new GenericFileAttachment();
        var tags = Enumerable.Range(0, ImageAnalysisApplier.MaxTags + 5).Select(i => ($"tag{i}", 0.6 + i / 100.0)).ToArray();

        ImageAnalysisApplier.Apply(attachment, Success(Caption, tags));

        var stored = attachment.GetTags();
        Assert.Equal(ImageAnalysisApplier.MaxTags, stored.Count);
        Assert.Equal($"tag{ImageAnalysisApplier.MaxTags + 4}", stored[0]);
    }

    [Fact]
    public void Drops_the_least_confident_tags_until_the_json_fits()
    {
        var attachment = new GenericFileAttachment();
        var tags = Enumerable.Range(0, ImageAnalysisApplier.MaxTags).Select(i => ($"{i:D2}{new string('x', 60)}", 0.99 - i / 100.0)).ToArray();

        ImageAnalysisApplier.Apply(attachment, Success(Caption, tags));

        var stored = attachment.GetTags();
        Assert.InRange(attachment.Tags!.Length, 1, ImageAnalysisApplier.MaxTagsLength);
        Assert.InRange(stored.Count, 1, ImageAnalysisApplier.MaxTags - 1);
        Assert.Equal(tags.Take(stored.Count).Select(t => t.Item1), stored);
    }

    [Fact]
    public void Trims_the_caption_to_the_description_limit()
    {
        var attachment = new GenericFileAttachment();

        ImageAnalysisApplier.Apply(attachment, Success(new string('x', ImageAnalysisApplier.MaxDescriptionLength + 50)));

        Assert.Equal(ImageAnalysisApplier.MaxDescriptionLength, attachment.Description!.Length);
    }

    [Fact]
    public void A_blank_caption_clears_the_description()
    {
        var attachment = new GenericFileAttachment { Description = "old" };

        ImageAnalysisApplier.Apply(attachment, Success("   "));

        Assert.Null(attachment.Description);
    }

    [Fact]
    public void A_failed_result_changes_nothing()
    {
        var attachment = new GenericFileAttachment { Description = "old" };
        attachment.SetTags(["kept"]);
        var failed = new VisionAnalysisResult { IsSuccess = false, ErrorMessage = "boom", Caption = Caption, Tags = [new() { Name = "dog", Confidence = 0.9 }] };

        Assert.False(ImageAnalysisApplier.Apply(attachment, failed));

        Assert.Equal("old", attachment.Description);
        Assert.Equal(["kept"], attachment.GetTags());
    }

    private static VisionAnalysisResult Success(string? caption, params (string Name, double Confidence)[] tags) => new()
    {
        IsSuccess = true,
        Caption = caption,
        Tags = [.. tags.Select(t => new VisionTag { Name = t.Name, Confidence = t.Confidence })],
    };
}
