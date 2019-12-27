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
        public AnalyzeImageForDescriptionAttribute(string languageCode = null, bool upperCaseFirstLetter = true, bool endWithDot = true) : base(languageCode)
        {
            UpperCaseFirstLetter = upperCaseFirstLetter;
            EndWithDot = endWithDot;
        }

        public bool UpperCaseFirstLetter { get; set; }
        public bool EndWithDot { get; set; }
    }
}