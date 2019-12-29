using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Gulla.EpiserverAutomaticImageDescription.Core.Translation;
using Gulla.EpiserverAutomaticImageDescription.Core.Translation.Constants;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;

namespace Gulla.EpiserverAutomaticImageDescription.Core.Image.Attributes
{
    /// <summary>
    /// Analyze image and create a list of tags. Apply to string or IList&lt;string&gt; properties.
    /// </summary>
    public class AnalyzeImageForTagsAttribute : BaseImageDetailsAttribute
    {
        /// <summary>
        /// Analyze image and create a list of tags. Apply to string or IList&lt;string&gt; properties.
        /// </summary>
        /// <param name="languageCode">Translate tags to specified language.</param>
        public AnalyzeImageForTagsAttribute(string languageCode = null)
        {
            LanguageCode = languageCode;
        }

        private string LanguageCode { get; }

        public override bool AnalyzeImageContent => true;

        public override void Update(object content, ImageAnalysis imageAnalyzerResult, OcrResult ocrResult, PropertyInfo propertyInfo, TranslationService translationService)
        {
            if (imageAnalyzerResult.Tags == null || imageAnalyzerResult.Tags.Count == 0)
            {
                return;
            }

            var tags = GetTranslatedTags(imageAnalyzerResult.Tags.Select(x => x.Name), translationService);

            if (IsStringProperty(propertyInfo))
            {
                propertyInfo.SetValue(content, string.Join(", ", tags));
            }
            else if (IsStringListProperty(propertyInfo))
            {
                propertyInfo.SetValue(content, tags.ToList());
            }
        }

        private IEnumerable<string> GetTranslatedTags(IEnumerable<string> tags, TranslationService translationService)
        {
            if (LanguageCode == null)
            {
                return tags;
            }

            return translationService.TranslateText(tags, LanguageCode, TranslationLanguage.English);
        }
    }
}