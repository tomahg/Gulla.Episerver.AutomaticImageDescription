using System;

namespace Gulla.EpiserverAutomaticImageDescription.Core.ImageAnalysis.Attributes
{
    public abstract class BaseImageDetailsAttribute : Attribute
    {
        protected BaseImageDetailsAttribute()
        {

        }

        protected BaseImageDetailsAttribute(string languageCode)
        {
            LanguageCode = languageCode;
        }

        public string LanguageCode { get; set; }
    }
}