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
    /// Analyze image and create a list of tags. Apply to string, IList&lt;string&gt;, LocalizedString or LocalizedStringList properties.
    /// For LocalizedString or LocalizedStringList, TranslationLanguage.AllActive or comma-separated list of language ids can be used.
    /// </summary>
    public class AnalyzeImageForTagsAttribute : BaseImageDetailsAttribute
    {
        private readonly string _languageCode;

        /// <summary>
        /// Analyze image and create a list of tags. Apply to string, IList&lt;string&gt;, LocalizedString or LocalizedStringList properties.
        /// </summary>
        /// <param name="languageCode">Translate tags to specified language.</param>
        public AnalyzeImageForTagsAttribute(string languageCode = null)
        {
            _languageCode = languageCode;
        }

        public override bool AnalyzeImageContent => true;

        public override bool RequireTranslations => _languageCode != null;

        public override void Update(PropertyAccess propertyAccess, ImageAnalysis imageAnalyzerResult, OcrResult ocrResult, TranslationService translationService)
        {
            if (imageAnalyzerResult.Tags == null || imageAnalyzerResult.Tags.Count == 0)
            {
                return;
            }

            var tags = imageAnalyzerResult.Tags.Select(x => x.Name);

            if (IsStringProperty(propertyAccess.Property))
            {
                var translatedTags = GetTranslatedTags(tags, translationService);
                propertyAccess.SetValue(string.Join(", ", translatedTags));
            }
            else if (IsStringListProperty(propertyAccess.Property))
            {
                var translatedTags = GetTranslatedTags(tags, translationService);
                propertyAccess.SetValue(translatedTags.ToList());
            }
            else if (IsLocalizedStringProperty(propertyAccess.Property))
            {
                propertyAccess.SetValue(GetTranslatedLocalizedStrings(tags.ToList(), GetLanguageCodes(), translationService));
            }
            else if (IsLocalizedStringListProperty(propertyAccess.Property))
            {
                propertyAccess.SetValue(GetTranslatedLocalizedStringLists(tags.ToList(), GetLanguageCodes(), translationService));
            }
        }

        private IEnumerable<string> GetTranslatedTags(IEnumerable<string> tags, TranslationService translationService)
        {
            return _languageCode == null ? tags : translationService.TranslateText(tags, _languageCode, TranslationLanguage.English);
        }

        private static IEnumerable<LocalizedString> GetTranslatedLocalizedStrings(IList<string> tags, IEnumerable<string> languageCodes, TranslationService translationService)
        {
            return languageCodes.Select(languageCode => GetTranslatedLocalizedString(tags, languageCode, translationService)).ToList();
        }

        private static LocalizedString GetTranslatedLocalizedString(IEnumerable<string> tags, string languageCode, TranslationService translationService)
        {
            if (languageCode == TranslationLanguage.English)
            {
                return new LocalizedString { Language = TranslationLanguage.English, Value = string.Join(", ", tags) };
            }

            return new LocalizedString { Language = languageCode, Value = string.Join(", " , GetTranslatedTags(translationService, tags, languageCode)) };
        }

        private static IEnumerable<LocalizedStringList> GetTranslatedLocalizedStringLists(IList<string> tags, IEnumerable<string> languageCodes, TranslationService translationService)
        {
            return languageCodes.Select(languageCode => GetTranslatedLocalizedStringList(tags, languageCode, translationService)).ToList();
        }

        private static LocalizedStringList GetTranslatedLocalizedStringList(IList<string> tags, string languageCode, TranslationService translationService)
        {
            if (languageCode == TranslationLanguage.English)
            {
                return new LocalizedStringList { Language = TranslationLanguage.English, Value = tags };
            }

            return new LocalizedStringList { Language = languageCode, Value = GetTranslatedTags(translationService, tags, languageCode).ToList() };
        }

        private static IEnumerable<string> GetTranslatedTags(TranslationService translationService, IEnumerable<string> tags, string toLanguage)
        {
            return translationService.TranslateText(tags, toLanguage, TranslationLanguage.English).Select(x => x.ToLower());
        }

        private IEnumerable<string> GetLanguageCodes()
        {
            var languageCodes = _languageCode?.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            if (_languageCode == TranslationLanguage.AllActive)
            {
               return new LanguageSelectionFactory().GetSelections(null).Select(x => x.Value as string).ToList();
            }

            if (languageCodes?.Any() != true)
            {
                return new List<string>() { TranslationLanguage.English };
            }

            return languageCodes;
        }
    }
}