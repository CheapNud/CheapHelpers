using Azure;
using Azure.AI.Vision.ImageAnalysis;
using CheapHelpers.Services.Vision;
using Microsoft.Extensions.Options;
using System.Diagnostics;

/// <summary>
/// Azure Cognitive Services Vision API implementation using correct 1.0.0 API
/// </summary>
public class AzureVisionService : IVisionService
{
    private readonly VisionServiceOptions _options;
    private readonly ImageAnalysisClient _client;

    public AzureVisionService(IOptions<VisionServiceOptions> options)
    {
        _options = options.Value;

        if (string.IsNullOrEmpty(_options.Endpoint) || string.IsNullOrEmpty(_options.ApiKey))
        {
            throw new InvalidOperationException("Azure Vision service endpoint and API key must be configured");
        }

        _client = new ImageAnalysisClient(
            new Uri(_options.Endpoint),
            new AzureKeyCredential(_options.ApiKey));
    }

    public async Task<VisionAnalysisResult> AnalyzeImageAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            Debug.WriteLine($"Analyzing image from URL: {imageUrl}");

            var visualFeatures = VisualFeatures.Caption |
                               VisualFeatures.Read |
                               VisualFeatures.Tags |
                               VisualFeatures.Objects;

            var analysisOptions = new ImageAnalysisOptions
            {
                GenderNeutralCaption = _options.GenderNeutralCaption,
                Language = _options.DefaultLanguage
            };

            var result = await _client.AnalyzeAsync(
                new Uri(imageUrl),
                visualFeatures,
                analysisOptions,
                cancellationToken);

            return ProcessAnalysisResult(result);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error analyzing image from URL: {ex.Message}");
            return new VisionAnalysisResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<VisionAnalysisResult> AnalyzeImageAsync(Stream imageStream, CancellationToken cancellationToken = default)
    {
        try
        {
            Debug.WriteLine("Analyzing image from stream");

            var visualFeatures = VisualFeatures.Caption |
                               VisualFeatures.Read |
                               VisualFeatures.Tags |
                               VisualFeatures.Objects;

            var analysisOptions = new ImageAnalysisOptions
            {
                GenderNeutralCaption = _options.GenderNeutralCaption,
                Language = _options.DefaultLanguage
            };

            var imageData = BinaryData.FromStream(imageStream);

            var result = await _client.AnalyzeAsync(
                imageData,
                visualFeatures,
                analysisOptions,
                cancellationToken);

            return ProcessAnalysisResult(result);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error analyzing image from stream: {ex.Message}");
            return new VisionAnalysisResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private static VisionAnalysisResult ProcessAnalysisResult(ImageAnalysisResult result)
    {
        var analysisResult = new VisionAnalysisResult { IsSuccess = true };

        // Extract caption
        if (result.Caption != null)
        {
            analysisResult.Caption = result.Caption.Text;
            analysisResult.CaptionConfidence = result.Caption.Confidence;
            Debug.WriteLine($"Caption: {result.Caption.Text} (Confidence: {result.Caption.Confidence:F2})");
        }

        // Extract text (OCR)
        if (result.Read?.Blocks != null)
        {
            foreach (var block in result.Read.Blocks)
            {
                foreach (var line in block.Lines)
                {
                    analysisResult.ExtractedText.Add(line.Text);
                    Debug.WriteLine($"Text: {line.Text}");
                }
            }
        }

        // Extract tags
        if (result.Tags?.Values != null)
        {
            foreach (var tag in result.Tags.Values)
            {
                if (tag.Confidence > 0.5) // Only include confident tags
                {
                    analysisResult.Tags.Add(new VisionTag
                    {
                        Name = tag.Name,
                        Confidence = tag.Confidence
                    });
                    Debug.WriteLine($"Tag: {tag.Name} (Confidence: {tag.Confidence:F2})");
                }
            }
        }

        // Extract objects
        if (result.Objects?.Values != null)
        {
            foreach (var obj in result.Objects.Values)
            {
                // Objects in Azure Vision don't have direct confidence, use tag confidence
                var primaryTag = obj.Tags?.FirstOrDefault();
                if (primaryTag != null && primaryTag.Confidence > 0.5)
                {
                    analysisResult.Objects.Add(new VisionDetectedObject
                    {
                        Name = primaryTag.Name,
                        Confidence = primaryTag.Confidence,
                        BoundingBox = new VisionBoundingBox
                        {
                            X = obj.BoundingBox.X,
                            Y = obj.BoundingBox.Y,
                            Width = obj.BoundingBox.Width,
                            Height = obj.BoundingBox.Height
                        }
                    });
                    Debug.WriteLine($"Object: {primaryTag.Name} (Confidence: {primaryTag.Confidence:F2})");
                }
            }
        }

        return analysisResult;
    }
}