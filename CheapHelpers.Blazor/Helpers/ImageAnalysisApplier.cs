using CheapHelpers.Models.Entities;
using CheapHelpers.Services.Vision;

namespace CheapHelpers.Blazor.Helpers;

/// <summary>
/// Copies a vision analysis onto a file attachment: the caption into <see cref="FileAttachment.Description"/> and the confident tags into <see cref="FileAttachment.Tags"/>.
/// </summary>
internal static class ImageAnalysisApplier
{
    /// <summary>Lowest tag confidence that is kept.</summary>
    internal const double MinTagConfidence = 0.5;

    /// <summary>Most tags stored on one attachment.</summary>
    internal const int MaxTags = 20;

    /// <summary>Length limit of <see cref="FileAttachment.Description"/>.</summary>
    internal const int MaxDescriptionLength = 500;

    /// <summary>
    /// Applies <paramref name="result"/> to <paramref name="attachment"/>; returns false and changes nothing when the analysis failed.
    /// </summary>
    internal static bool Apply(FileAttachment attachment, VisionAnalysisResult result)
    {
        if (!result.IsSuccess)
            return false;

        var caption = result.Caption?.Trim();
        attachment.Description = string.IsNullOrEmpty(caption)
            ? null
            : caption[..Math.Min(caption.Length, MaxDescriptionLength)];

        attachment.SetTags(result.Tags
            .Where(t => t.Confidence >= MinTagConfidence && !string.IsNullOrWhiteSpace(t.Name))
            .OrderByDescending(t => t.Confidence)
            .Select(t => t.Name)
            .Distinct()
            .Take(MaxTags));

        return true;
    }
}
