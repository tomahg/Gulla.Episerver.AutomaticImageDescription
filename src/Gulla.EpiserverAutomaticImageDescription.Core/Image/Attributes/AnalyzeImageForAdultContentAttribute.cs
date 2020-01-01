using System.Globalization;
using Gulla.EpiserverAutomaticImageDescription.Core.Translation;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;

namespace Gulla.EpiserverAutomaticImageDescription.Core.Image.Attributes
{
    /// <summary>
    /// Analyze image for adult content. Apply to bool properties for true/false or double/string for adult score.
    /// Adult images are defined as those which are explicitly sexual in nature and often depict nudity and sexual acts.
    /// </summary>
    public class AnalyzeImageForAdultContentAttribute : BaseImageDetailsAttribute
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
                propertyAccess.SetValue(imageAnalyzerResult.Adult.IsAdultContent);
            }
            else if (IsDoubleProperty(propertyAccess.Property))
            {
                propertyAccess.SetValue(imageAnalyzerResult.Adult.AdultScore);
            }
            else if (IsStringProperty(propertyAccess.Property))
            {
                propertyAccess.SetValue(imageAnalyzerResult.Adult.AdultScore.ToString(CultureInfo.InvariantCulture));
            }
        }
    }
}