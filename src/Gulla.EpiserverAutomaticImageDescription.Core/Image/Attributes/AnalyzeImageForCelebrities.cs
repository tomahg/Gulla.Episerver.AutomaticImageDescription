using System.Linq;
using Gulla.Episerver.AutomaticImageDescription.Core.Translation;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;

namespace Gulla.Episerver.AutomaticImageDescription.Core.Image.Attributes
{
    /// <summary>
    /// Analyze image for celebrities. Apply to string or IList&lt;string&gt; properties.
    /// </summary>
    public class AnalyzeImageForCelebrities : BaseImageDetailsAttribute
    {
        public override bool AnalyzeImageContent => true;

        public override void Update(PropertyAccess propertyAccess, ImageAnalysis imageAnalyzerResult, OcrResult ocrResult, TranslationService translationService)
        {
            if (imageAnalyzerResult?.Categories == null)
            {
                return;
            }

            var celebrities = imageAnalyzerResult.Categories.Where(x => x.Detail != null).Select(x => x.Detail.Celebrities).Where(x => x != null).SelectMany(x => x).Select(y => y.Name).ToList();
            if (!celebrities.Any())
            {
                return;
            }

            if (IsStringProperty(propertyAccess.Property))
            {
                propertyAccess.SetValue(string.Join(", ", celebrities));
            }
            else if (IsStringListProperty(propertyAccess.Property))
            {
                propertyAccess.SetValue(celebrities.ToList());
            }
        }
    }
}