namespace Gulla.EpiserverAutomaticImageDescription.Core.ImageAnalysis.Attributes
{
    public class AnalyzeImageForOcrAttribute : BaseImageDetailsAttribute
    {
        public AnalyzeImageForOcrAttribute()
        {
        }

        public AnalyzeImageForOcrAttribute(string toLanguage, string fromLanguage = null)
        {
            LanguageCode = toLanguage;
            FromLanguageCode = fromLanguage;
        }

        public string FromLanguageCode { get; }
    }
}