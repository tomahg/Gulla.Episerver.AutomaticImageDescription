using System.Globalization;
using Gulla.EpiserverAutomaticImageDescription.Core.Translation;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;

namespace Gulla.EpiserverAutomaticImageDescription.Core.Image.Attributes
{
    /// <summary>
    /// Analyze image for racy content. Apply to bool properties for true/false or double/string for racy score.
    /// Racy images are defined as images that are sexually suggestive in nature and often contain less sexually explicit content than images tagged as Adult.
    /// </summary>
    public class AnalyzeImageForRacyContentAttribute : BaseImageDetailsAttribute
    {
        public override bool AnalyzeImageContent => true;

        public override void Update(PropertyAccess propertyAccess, ImageAnalysis imageAnalyzerResult, OcrResult ocrResult, TranslationService translationService)
        {
            if (imageAnalyzerResult.Adult == null)
            {
                return;
            }

            if (IsBooleanProperty(propertyAccess.Property))
            {
                propertyAccess.SetPropertyValue(imageAnalyzerResult.Adult.IsRacyContent);
            }
            else if (IsDoubleProperty(propertyAccess.Property))
            {
                propertyAccess.SetPropertyValue(imageAnalyzerResult.Adult.RacyScore);
            }
            else if (IsStringProperty(propertyAccess.Property))
            {
                propertyAccess.SetPropertyValue(imageAnalyzerResult.Adult.RacyScore.ToString(CultureInfo.InvariantCulture));
            }
        }
    }
}