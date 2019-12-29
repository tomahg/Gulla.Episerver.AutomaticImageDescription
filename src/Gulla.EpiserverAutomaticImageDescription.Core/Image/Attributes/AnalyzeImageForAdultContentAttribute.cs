using System.Globalization;
using System.Reflection;
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

        public override void Update(object content, ImageAnalysis imageAnalyzerResult, OcrResult ocrResult, PropertyInfo propertyInfo, TranslationService translationService)
        {
            if (imageAnalyzerResult.Adult == null)
            {
                return;
            }

            if (IsBooleanProperty(propertyInfo))
            {
                propertyInfo.SetValue(content, imageAnalyzerResult.Adult.IsAdultContent);
            }
            else if (IsDoubleProperty(propertyInfo))
            {
                propertyInfo.SetValue(content, imageAnalyzerResult.Adult.AdultScore);
            }
            else if (IsStringProperty(propertyInfo))
            {
                propertyInfo.SetValue(content, imageAnalyzerResult.Adult.AdultScore.ToString(CultureInfo.InvariantCulture));
            }
        }
    }
}