using Azure.AI.Vision.ImageAnalysis;

namespace CheapHelpers.Services.Vision
{
    /// <summary>
    /// Configuration options for Azure Vision Service
    /// </summary>
    public class VisionServiceOptions
    {
        public const string SectionName = "AzureVision";

        public string Endpoint { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string DefaultLanguage { get; set; } = "en";
        public bool GenderNeutralCaption { get; set; } = true;
    }
}