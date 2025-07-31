namespace CheapHelpers.Services.Vision
{
    /// <summary>
    /// Vision service tag with confidence
    /// </summary>
    public class VisionTag
    {
        public string Name { get; set; } = string.Empty;
        public double Confidence { get; set; }
    }
}
