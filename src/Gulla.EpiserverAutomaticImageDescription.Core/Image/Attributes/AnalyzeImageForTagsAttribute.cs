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

        public override void Update(object content, ImageAnalysis imageAnalyzerResult, OcrResult ocrResult, PropertyInfo propertyInfo)
        {
            if (imageAnalyzerResult.Description.Tags == null || imageAnalyzerResult.Description.Tags.Count == 0)
            {
                return;
            }

            var tagsTranslated = GetTranslatedTags(imageAnalyzerResult.Description.Tags, propertyInfo);

            if (IsStringProperty(propertyInfo))
            {
                propertyInfo.SetValue(content, string.Join(", ", tagsTranslated));
            }
            else if (IsStringListProperty(propertyInfo))
            {
                propertyInfo.SetValue(content, tagsTranslated.ToList());
            }
        }

        private static IEnumerable<string> GetTranslatedTags(IEnumerable<string> tags, PropertyInfo propertyInfo)
        {
            var languageCode = propertyInfo.GetCustomAttribute<AnalyzeImageForTagsAttribute>().LanguageCode;
            if (languageCode == null)
            {
                return tags;
            }

            return Translator.TranslateText(tags, languageCode, TranslationLanguage.English).Select(x => x.Translations).Select(x => x.First().Text);
        }
    }
}