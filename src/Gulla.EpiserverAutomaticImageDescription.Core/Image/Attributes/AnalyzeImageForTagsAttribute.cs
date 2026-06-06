using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.Logging;
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
        private static readonly ILogger Log = LogManager.GetLogger(typeof(AnalyzeImageForTagsAttribute));
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

        public override void Update(PropertyAccess propertyAccess, ImageAnalysis imageAnalyzerResult, ReadResult readResult, TranslationService translationService)
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
            if (_languageCode == null)
            {
                return tags;
            }

            try
            {
                return translationService.TranslateText(tags, _languageCode, TranslationLanguage.English);
            }
            catch (Exception e)
            {
                var message = (e as AggregateException)?.InnerException?.Message ?? e.Message;
                Log.Warning($"Failed to translate tags to language '{_languageCode}', returning untranslated tags. {message}");
                return tags;
            }
        }

        private IEnumerable<LocalizedString> GetTranslatedLocalizedStrings(IList<string> tags, IEnumerable<string> languageCodes, TranslationService translationService)
        {
            return languageCodes.Select(languageCode => GetTranslatedLocalizedString(tags, languageCode, translationService)).Where(x => x != null).ToList();
        }

        private LocalizedString GetTranslatedLocalizedString(IEnumerable<string> tags, string languageCode, TranslationService translationService)
        {
            if (languageCode == TranslationLanguage.English)
            {
                return new LocalizedString { Language = TranslationLanguage.English, Value = string.Join(", ", tags) };
            }

            try
            {
                return new LocalizedString { Language = languageCode, Value = string.Join(", ", GetTranslatedTags(translationService, tags, languageCode)) };
            }
            catch (Exception e)
            {
                var message = (e as AggregateException)?.InnerException?.Message ?? e.Message;
                Log.Warning($"Failed to translate tags to language '{languageCode}', using original value. {message}");
                return new LocalizedString { Language = languageCode, Value = string.Join(", ", tags) };
            }
        }

        private IEnumerable<LocalizedStringList> GetTranslatedLocalizedStringLists(IList<string> tags, IEnumerable<string> languageCodes, TranslationService translationService)
        {
            return languageCodes.Select(languageCode => GetTranslatedLocalizedStringList(tags, languageCode, translationService)).Where(x => x != null).ToList();
        }

        private LocalizedStringList GetTranslatedLocalizedStringList(IList<string> tags, string languageCode, TranslationService translationService)
        {
            if (languageCode == TranslationLanguage.English)
            {
                return new LocalizedStringList { Language = TranslationLanguage.English, Value = tags };
            }

            try
            {
                return new LocalizedStringList { Language = languageCode, Value = GetTranslatedTags(translationService, tags, languageCode).ToList() };
            }
            catch (Exception e)
            {
                var message = (e as AggregateException)?.InnerException?.Message ?? e.Message;
                Log.Warning($"Failed to translate tags to language '{languageCode}', using original value. {message}");
                return new LocalizedStringList { Language = languageCode, Value = tags };
            }
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