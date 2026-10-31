using CheapHelpers.Services.Vision;

namespace CheapHelpers.Blazor.Tests;

/// <summary>
/// Vision service that never reaches Azure; the component tests only need one to be registered.
/// </summary>
internal sealed class FakeVisionService : IVisionService
{
    public Task<VisionAnalysisResult> AnalyzeImageAsync(string imageUrl, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<VisionAnalysisResult> AnalyzeImageAsync(Stream imageStream, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}
