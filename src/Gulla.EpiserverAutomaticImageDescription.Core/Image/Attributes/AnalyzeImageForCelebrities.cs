﻿using System.Linq;
using System.Reflection;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;

namespace Gulla.EpiserverAutomaticImageDescription.Core.Image.Attributes
{
    /// <summary>
    /// Analyze image for celebrities. Apply to string or IList&lt;string&gt; properties.
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

            var celebrities = imageAnalyzerResult.Categories.Where(x => x.Detail != null).Select(x => x.Detail.Celebrities).Where(x => x != null).SelectMany(x => x).Select(y => y.Name).ToList();
            if (!celebrities.Any())
            {
                return;
            }

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