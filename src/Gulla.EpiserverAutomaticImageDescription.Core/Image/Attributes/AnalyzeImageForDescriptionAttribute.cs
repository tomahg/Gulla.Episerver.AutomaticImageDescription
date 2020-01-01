using System.Linq;
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
        private readonly string _languageCode;
        private readonly bool _upperCaseFirstLetter;
        private readonly bool _endWithDot;

        /// <summary>
        /// Analyze image and create a description. Apply to string properties.
        /// </summary>
        /// <param name="languageCode">Translate description to specified language.</param>
        /// <param name="upperCaseFirstLetter">Uppercase first letter.</param>
        /// <param name="endWithDot">End description with a dot.</param>
        public AnalyzeImageForDescriptionAttribute(string languageCode = null, bool upperCaseFirstLetter = true, bool endWithDot = true)
        {
            _languageCode = languageCode;
            _upperCaseFirstLetter = upperCaseFirstLetter;
            _endWithDot = endWithDot;
        }

        public override bool AnalyzeImageContent => true;

        public override bool RequireTranslations => _languageCode != null;

        public override void Update(PropertyAccess propertyAccess, ImageAnalysis imageAnalyzerResult, OcrResult ocrResult, TranslationService translationService)
        {
            if (imageAnalyzerResult?.Description?.Captions == null || imageAnalyzerResult.Description.Captions.Count == 0)
            {
                return;
            }

            if (IsStringProperty(propertyAccess.Property))
            {
                var descriptionTranslated = GetTranslatedDescription(imageAnalyzerResult.Description.Captions.Select(caption => caption.Text).FirstOrDefault(), translationService);
                propertyAccess.SetPropertyValue(descriptionTranslated);
            }
        }

        private string GetTranslatedDescription(string description, TranslationService translationService)
        {
            if (_languageCode != null && !string.IsNullOrEmpty(description))
            {
                description = translationService.TranslateText(new[] { description }, _languageCode, TranslationLanguage.English).First();
            }

            return FormatDescription(description, _upperCaseFirstLetter, _endWithDot);
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