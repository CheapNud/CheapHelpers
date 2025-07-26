namespace CheapHelpers.Services.Vision
{
    /// <summary>
    /// Result of vision analysis operation
    /// </summary>
    public class VisionAnalysisResult
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public string? Caption { get; set; }
        public double CaptionConfidence { get; set; }
        public List<string> ExtractedText { get; set; } = [];
        public List<VisionTag> Tags { get; set; } = [];
        public List<VisionDetectedObject> Objects { get; set; } = [];
    }
}