namespace Gulla.EpiserverAutomaticImageDescription.Core.Image.Attributes
{
    /// <summary>
    /// Analyze image and perform Optical Character Recognition (OCR). Apply to string properties.
    /// </summary>
    public class AnalyzeImageForOcrAttribute : BaseImageDetailsAttribute
    {
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
            LanguageCode = toLanguage;
            FromLanguageCode = fromLanguage;
        }

        public string FromLanguageCode { get; }
    }
}