using System.Linq;
using System.Reflection;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;

namespace Gulla.EpiserverAutomaticImageDescription.Core.Image.Attributes
{
    /// <summary>
    /// Analyze image for known brands. Apply to string or IList&lt;string&gt; properties.
    /// </summary>
    public class AnalyzeImageForBrandsAttribute : BaseImageDetailsAttribute
    {
        public override bool AnalyzeImageContent => true;

        public override void Update(object content, ImageAnalysis imageAnalyzerResult, OcrResult ocrResult, PropertyInfo propertyInfo)
        {
            if (imageAnalyzerResult.Brands == null || imageAnalyzerResult.Brands.Count == 0)
            {
                return;
            }

            if (IsStringProperty(propertyInfo))
            {
                propertyInfo.SetValue(content, string.Join(", ", imageAnalyzerResult.Brands.Select(x => x.Name)));
            }
            else if (IsStringListProperty(propertyInfo))
            {
                propertyInfo.SetValue(content, imageAnalyzerResult.Brands.Select(x => x.Name).ToList());
            }
        }
    }
}