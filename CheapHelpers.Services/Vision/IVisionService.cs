namespace CheapHelpers.Services.Vision
{
    /// <summary>
    /// Interface for vision analysis service
    /// </summary>
    public interface IVisionService
    {
        Task<VisionAnalysisResult> AnalyzeImageAsync(string imageUrl, CancellationToken cancellationToken = default);
        Task<VisionAnalysisResult> AnalyzeImageAsync(Stream imageStream, CancellationToken cancellationToken = default);
    }
}