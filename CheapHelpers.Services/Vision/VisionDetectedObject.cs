namespace CheapHelpers.Services.Vision
{
    /// <summary>
    /// Detected object in image
    /// </summary>
    public class VisionDetectedObject
    {
        public string Name { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public VisionBoundingBox BoundingBox { get; set; } = new();
    }
}