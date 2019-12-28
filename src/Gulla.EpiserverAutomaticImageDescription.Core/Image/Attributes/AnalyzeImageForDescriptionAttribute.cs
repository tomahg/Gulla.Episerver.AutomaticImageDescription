using System.Linq;
using System.Reflection;
using Gulla.EpiserverAutomaticImageDescription.Core.Translation;
using Gulla.EpiserverAutomaticImageDescription.Core.Translation.Constants;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;

namespace Gulla.EpiserverAutomaticImageDescription.Core.Image.Attributes
{
    /// <summary>
    /// Analyze image and create a description. Apply to string properties.
    /// </summary>
    public class AnalyzeImageForDescriptionAttribute : BaseImageDetailsAttribute
    {
        /// <summary>
        /// Analyze image and create a description. Apply to string properties.
        /// </summary>
        /// <param name="languageCode">Translate description to specified language.</param>
        /// <param name="upperCaseFirstLetter">Uppercase first letter.</param>
        /// <param name="endWithDot">End description with a dot.</param>
        public AnalyzeImageForDescriptionAttribute(string languageCode = null, bool upperCaseFirstLetter = true, bool endWithDot = true)
        {
            LanguageCode = languageCode;
            UpperCaseFirstLetter = upperCaseFirstLetter;
            EndWithDot = endWithDot;
        }

        private string LanguageCode { get; }

        private bool UpperCaseFirstLetter { get; }

        private bool EndWithDot { get; }

        public override bool AnalyzeImageContent => true;

        public override void Update(object content, ImageAnalysis imageAnalyzerResult, OcrResult ocrResult, PropertyInfo propertyInfo)
        {
            if (imageAnalyzerResult?.Description?.Captions == null)
            {
                return;
            }

            if (IsStringProperty(propertyInfo))
            {
                var descriptionTranslated = GetTranslatedDescription(imageAnalyzerResult.Description.Captions.Select(caption => caption.Text).FirstOrDefault(), propertyInfo);
                propertyInfo.SetValue(content, descriptionTranslated);
            }
        }

        private static string GetTranslatedDescription(string description, PropertyInfo propertyInfo)
        {
            var descriptionAttribute = propertyInfo.GetCustomAttribute<AnalyzeImageForDescriptionAttribute>();

            if (descriptionAttribute.LanguageCode != null)
            {
                description = Translator.TranslateText(new[] { description }, descriptionAttribute.LanguageCode, TranslationLanguage.English).First().Translations.First().Text;
            }

            return FormatDescription(description, descriptionAttribute.UpperCaseFirstLetter, descriptionAttribute.EndWithDot);
        }

        private static string FormatDescription(string description, bool upperCaseFirstLetter, bool endWithDot)
        {
            if (string.IsNullOrEmpty(description))
            {
                return description;
            }

            if (upperCaseFirstLetter)
            {
                description = description[0].ToString().ToUpper() + (description.Length > 1 ? description.Substring(1) : "");
            }

            return description + (endWithDot && !description.Trim().EndsWith(".") ? "." : "");
        }
    }
}