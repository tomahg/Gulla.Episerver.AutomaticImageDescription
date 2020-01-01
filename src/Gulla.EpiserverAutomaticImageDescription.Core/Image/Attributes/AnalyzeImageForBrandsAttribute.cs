using System.Linq;
using Gulla.EpiserverAutomaticImageDescription.Core.Translation;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;

namespace Gulla.EpiserverAutomaticImageDescription.Core.Image.Attributes
{
    /// <summary>
    /// Analyze image for known brands. Apply to string or IList&lt;string&gt; properties.
    /// </summary>
    public class AnalyzeImageForBrandsAttribute : BaseImageDetailsAttribute
    {
        public override bool AnalyzeImageContent => true;

        public override void Update(PropertyAccess propertyAccess, ImageAnalysis imageAnalyzerResult, OcrResult ocrResult, TranslationService translationService)
        {
            if (imageAnalyzerResult.Brands == null || imageAnalyzerResult.Brands.Count == 0)
            {
                return;
            }

            if (IsStringProperty(propertyAccess.Property))
            {
                propertyAccess.SetPropertyValue(string.Join(", ", imageAnalyzerResult.Brands.Select(x => x.Name)));
            }
            else if (IsStringListProperty(propertyAccess.Property))
            {
                propertyAccess.SetPropertyValue(imageAnalyzerResult.Brands.Select(x => x.Name).ToList());
            }
        }
    }
}