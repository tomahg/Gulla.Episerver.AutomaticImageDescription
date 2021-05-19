using System.Globalization;
using Gulla.Episerver.AutomaticImageDescription.Core.Translation;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;

namespace Gulla.Episerver.AutomaticImageDescription.Core.Image.Attributes
{
    /// <summary>
    /// Analyze image for gory content. Apply to bool properties for true/false or double/string for gory score.
    /// Gory images show blood/gore.
    /// </summary>
    public class AnalyzeImageForGoryContentAttribute : BaseImageDetailsAttribute
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
                propertyAccess.SetValue(imageAnalyzerResult.Adult.IsGoryContent);
            }
            else if (IsDoubleProperty(propertyAccess.Property))
            {
                propertyAccess.SetValue(imageAnalyzerResult.Adult.GoreScore);
            }
            else if (IsStringProperty(propertyAccess.Property))
            {
                propertyAccess.SetValue(imageAnalyzerResult.Adult.GoreScore.ToString(CultureInfo.InvariantCulture));
            }
        }
    }
}