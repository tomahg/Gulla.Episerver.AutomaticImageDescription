using System;
using System.Collections.Generic;
using System.Linq;
using Gulla.Episerver.AutomaticImageDescription.Core.PropertyDefinitions;
using Gulla.Episerver.AutomaticImageDescription.Core.Translation;
using Gulla.Episerver.AutomaticImageDescription.Core.Translation.Constants;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;

namespace Gulla.Episerver.AutomaticImageDescription.Core.Image.Attributes
{
    /// <summary>
    /// Analyze image and create a description. Apply to string or localized string properties.
    /// </summary>
    public class AnalyzeImageForDescriptionAttribute : BaseImageDetailsAttribute
    {
        private readonly string _languageCode;
        private readonly bool _upperCaseFirstLetter;
        private readonly bool _endWithDot;

        /// <summary>
        /// Analyze image and create a description. Apply to string or localized string properties.
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

            var description = imageAnalyzerResult.Description.Captions.Select(caption => caption.Text).FirstOrDefault();

            if (IsStringProperty(propertyAccess.Property))
            {
                propertyAccess.SetValue(GetTranslatedDescription(description, translationService));
            }
            else if (IsLocalizedStringProperty(propertyAccess.Property))
            {
                propertyAccess.SetValue(GetTranslatedLocalizedStrings(description, GetLanguageCodes(), translationService));
            }
        }

        private string GetTranslatedDescription(string description, TranslationService translationService)
        {
            if (!string.IsNullOrEmpty(_languageCode) && _languageCode != TranslationLanguage.English && !string.IsNullOrEmpty(description))
            {
                description = translationService.TranslateText(new[] { description }, _languageCode, TranslationLanguage.English).First();
            }

            return FormatDescription(description, _upperCaseFirstLetter, _endWithDot);
        }

        private IEnumerable<LocalizedString> GetTranslatedLocalizedStrings(string description, IEnumerable<string> languageCodes, TranslationService translationService)
        {
            return languageCodes.Select(languageCode => GetTranslatedLocalizedString(description, languageCode, translationService)).ToList();
        }

        private LocalizedString GetTranslatedLocalizedString(string description, string languageCode, TranslationService translationService)
        {
            if (languageCode == TranslationLanguage.English)
            {
                var formattedDescription = FormatDescription(description, _upperCaseFirstLetter, _endWithDot);
                return new LocalizedString { Language = TranslationLanguage.English, Value = formattedDescription };
            }

            var translatedDescription = translationService.TranslateText(new[] { description }, languageCode, TranslationLanguage.English).First();
            var formattedTranslatedDescription = FormatDescription(translatedDescription, _upperCaseFirstLetter, _endWithDot);
            return new LocalizedString { Language = languageCode, Value = formattedTranslatedDescription };
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

        private IEnumerable<string> GetLanguageCodes()
        {
            var languageCodes = _languageCode?.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            if (_languageCode == TranslationLanguage.AllActive || languageCodes?.Any() != true)
            {
                languageCodes = new LanguageSelectionFactory().GetSelections(null).Select(x => x.Value as string).ToList();
            }

            return languageCodes;
        }
    }
}