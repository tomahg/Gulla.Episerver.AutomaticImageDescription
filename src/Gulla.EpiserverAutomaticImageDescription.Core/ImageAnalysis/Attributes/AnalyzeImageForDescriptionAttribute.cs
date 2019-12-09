namespace Gulla.EpiserverAutomaticImageDescription.Core.ImageAnalysis.Attributes
{
    public class AnalyzeImageForDescriptionAttribute : BaseImageDetailsAttribute
    {
        public AnalyzeImageForDescriptionAttribute(string languageCode = null, bool upperCaseFirstLetter = true, bool endWithDot = true) : base(languageCode)
        {
            UpperCaseFirstLetter = upperCaseFirstLetter;
            EndWithDot = endWithDot;
        }

        public bool UpperCaseFirstLetter { get; set; }
        public bool EndWithDot { get; set; }
    }
}