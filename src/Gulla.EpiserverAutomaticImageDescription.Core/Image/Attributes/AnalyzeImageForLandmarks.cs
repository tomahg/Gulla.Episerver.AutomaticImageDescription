using System.Linq;
using System.Reflection;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;

namespace Gulla.EpiserverAutomaticImageDescription.Core.Image.Attributes
{
    /// <summary>
    /// Analyze image for landmarks. Apply to string or IList&lt;string&gt; properties.
    /// </summary>
    public class AnalyzeImageForLandmarks : BaseImageDetailsAttribute
    {
        public override bool AnalyzeImageContent => true;

        public override void Update(object content, ImageAnalysis imageAnalyzerResult, OcrResult ocrResult, PropertyInfo propertyInfo)
        {
            if (imageAnalyzerResult?.Categories == null)
            {
                return;
            }

            var landmarks = imageAnalyzerResult.Categories.Select(x => x.Detail?.Landmarks).SelectMany(x => x).Select(y => y.Name).ToList();
            if (!landmarks.Any())
            {
                return;
            }

            if (IsStringProperty(propertyInfo))
            {
                propertyInfo.SetValue(content, string.Join(", ", landmarks));
            }
            else if (IsStringListProperty(propertyInfo))
            {
                propertyInfo.SetValue(content, landmarks.ToList());
            }
        }
    }
}