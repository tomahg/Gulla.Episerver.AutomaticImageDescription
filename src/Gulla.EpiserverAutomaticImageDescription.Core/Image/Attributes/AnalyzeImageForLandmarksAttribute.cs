using System.Linq;
using Gulla.Episerver.AutomaticImageDescription.Core.Translation;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;

namespace Gulla.Episerver.AutomaticImageDescription.Core.Image.Attributes
{
    /// <summary>
    /// Analyze image for landmarks. Apply to string or IList&lt;string&gt; properties.
    /// </summary>
    public class AnalyzeImageForLandmarksAttribute : BaseImageDetailsAttribute
    {
        public override bool AnalyzeImageContent => true;

        public override void Update(PropertyAccess propertyAccess, ImageAnalysis imageAnalyzerResult, OcrResult ocrResult, TranslationService translationService)
        {
            if (imageAnalyzerResult?.Categories == null)
            {
                return;
            }

            var landmarks = imageAnalyzerResult.Categories.Where(x => x.Detail != null).Select(x => x.Detail.Landmarks).Where(x => x != null).SelectMany(x => x).Select(y => y.Name).ToList();
            if (!landmarks.Any())
            {
                return;
            }

            if (IsStringProperty(propertyAccess.Property))
            {
                propertyAccess.SetValue(string.Join(", ", landmarks));
            }
            else if (IsStringListProperty(propertyAccess.Property))
            {
                propertyAccess.SetValue(landmarks.ToList());
            }
        }
    }
}