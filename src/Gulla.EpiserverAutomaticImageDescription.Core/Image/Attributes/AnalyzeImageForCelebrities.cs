using System.Linq;
using System.Reflection;
using Gulla.EpiserverAutomaticImageDescription.Core.Translation;
using Gulla.EpiserverAutomaticImageDescription.Core.Translation.Constants;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;

namespace Gulla.EpiserverAutomaticImageDescription.Core.Image.Attributes
{
    /// <summary>
    /// Analyze image for celebrities. Apply to string properties.
    /// </summary>
    public class AnalyzeImageForCelebrities : BaseImageDetailsAttribute
    {
        public override bool AnalyzeImageContent => true;

        public override void Update(object content, ImageAnalysis imageAnalyzerResult, OcrResult ocrResult, PropertyInfo propertyInfo)
        {
            if (imageAnalyzerResult?.Categories == null)
            {
                return;
            }

            var celebrities = imageAnalyzerResult.Categories.Select(x => x.Detail?.Celebrities).SelectMany(x => x).Select(y => y.Name);
            
            if (IsStringProperty(propertyInfo))
            {
                propertyInfo.SetValue(content, string.Join(", ", celebrities));
            }
            else if (IsStringListProperty(propertyInfo))
            {
                propertyInfo.SetValue(content, celebrities.ToList());
            }
        }
    }
}