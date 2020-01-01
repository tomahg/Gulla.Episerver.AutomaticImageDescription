using System.Collections.Generic;
using System.Linq;
using Gulla.EpiserverAutomaticImageDescription.Core.Translation;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;

namespace Gulla.EpiserverAutomaticImageDescription.Core.Image.Attributes
{
    /// <summary>
    /// Analyze image and perform Optical Character Recognition (OCR). Apply to string properties.
    /// </summary>
    public class AnalyzeImageForOcrAttribute : BaseImageDetailsAttribute
    {
        private readonly string _fromLanguageCode;
        private readonly string _toLanguageCode;

        public AnalyzeImageForOcrAttribute()
        {
        }

        /// <summary>
        /// Analyze image and perform Optical Character Recognition (OCR). Apply to string properties.
        /// </summary>
        /// <param name="toLanguage">Translate OCR result to specified language.</param>
        /// <param name="fromLanguage">If you know what language the source text is, specify. If null, the algorithm will try to detect source language.</param>
        public AnalyzeImageForOcrAttribute(string toLanguage, string fromLanguage = null)
        {
            _toLanguageCode = toLanguage;
            _fromLanguageCode = fromLanguage;
        }

        public override bool AnalyzeImageOcr => true;
        public override bool RequireTranslations => _toLanguageCode != null;

        public override void Update(PropertyAccess propertyAccess, ImageAnalysis imageAnalyzerResult, OcrResult ocrResult, TranslationService translationService)
        {
            if (ocrResult.Regions == null || ocrResult.Regions.Count == 0)
            {
                return;
            }

            var ocrTranslated = GetTranslatedOcr(ocrResult, translationService);

            if (IsStringProperty(propertyAccess.Property))
            {
                propertyAccess.SetPropertyValue(string.Join(" ", ocrTranslated));
            }
        }

        private IEnumerable<string> GetTranslatedOcr(OcrResult ocrResult, TranslationService translationService)
        {
            var words = ocrResult.Regions.Select(x => x.Lines).SelectMany(x => x).Select(x => x.Words).SelectMany(x => x).Select(x => x.Text).ToList();
            if (_toLanguageCode == null || words.Count == 0)
            {
                return words;
            }

            return translationService.TranslateText(words, _toLanguageCode, _fromLanguageCode);
        }
    }
}