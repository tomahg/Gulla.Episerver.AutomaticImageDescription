using System.Collections.Generic;
using System.Linq;
using Gulla.Episerver.AutomaticImageDescription.Core.Translation;
using Gulla.Episerver.AutomaticImageDescription.Core.Translation.Constants;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;

namespace Gulla.Episerver.AutomaticImageDescription.Core.Image.Attributes
{
    /// <summary>
    /// Analyze image for faces. Apply to string or IList&lt;string&gt; properties.
    /// </summary>
    public class AnalyzeImageForFacesAttribute : BaseImageDetailsAttribute
    {
        private readonly string _languageCode;
        private readonly string _maleAdultString;
        private readonly string _femaleAdultString;
        private readonly string _otherAdultString;
        private readonly string _maleChildString;
        private readonly string _femaleChildString;
        private readonly string _otherChildString;
        private readonly int _childTurnsAdultAtAge;
        private readonly bool _genderValuesSpecified;

        /// <summary>
        /// Analyze image for faces. Use generic names for gender. Apply to string or IList&lt;string&gt; properties.
        /// </summary>
        /// <param name="languageCode">Translate tags to specified language.</param>
        public AnalyzeImageForFacesAttribute(string languageCode = null)
        {
            _languageCode = languageCode;
        }

        /// <summary>
        /// Analyze image for faces. Supply your own names for gender, child and adult, and cutoff age. Apply to string or IList&lt;string&gt; properties.
        /// </summary>
        /// <param name="maleAdultString"></param>
        /// <param name="femaleAdultString"></param>
        /// <param name="otherAdultString"></param>
        /// <param name="maleChildString"></param>
        /// <param name="femaleChildString"></param>
        /// <param name="otherChildString"></param>
        /// <param name="childTurnsAdultAtAge"></param>
        public AnalyzeImageForFacesAttribute(string maleAdultString, string femaleAdultString, string otherAdultString, string maleChildString, string femaleChildString, string otherChildString, int childTurnsAdultAtAge)
        {
            _genderValuesSpecified = true;
            _maleAdultString = maleAdultString;
            _femaleAdultString = femaleAdultString;
            _otherAdultString = otherAdultString;
            _maleChildString = maleChildString;
            _femaleChildString = femaleChildString;
            _otherChildString = otherChildString;
            _childTurnsAdultAtAge = childTurnsAdultAtAge;
        }

        public override bool AnalyzeImageContent => true;

        public override bool RequireTranslations => _languageCode != null;

        public override void Update(PropertyAccess propertyAccess, ImageAnalysis imageAnalyzerResult, OcrResult ocrResult, TranslationService translationService)
        {
            if (imageAnalyzerResult.Faces == null || imageAnalyzerResult.Faces.Count == 0)
            {
                return;
            }

            IEnumerable<string> faces;

            if (_genderValuesSpecified)
            {
                faces = GetFacesWithSpecifiedValued(imageAnalyzerResult.Faces);
            }
            else
            {
                faces = imageAnalyzerResult.Faces.Select(x => $"{GetTranslatedGender(x.Gender, translationService)} ({x.Age})");
            }

            if (IsStringProperty(propertyAccess.Property))
            {
                propertyAccess.SetValue(string.Join(", ", faces));
            }
            else if (IsStringListProperty(propertyAccess.Property))
            {
                propertyAccess.SetValue(faces.ToList());
            }
        }

        private IEnumerable<string> GetFacesWithSpecifiedValued(IList<FaceDescription> faces)
        {
            foreach (var faceDescription in faces)
            {
                if (faceDescription.Age >= _childTurnsAdultAtAge)
                {
                    yield return (faceDescription.Gender.HasValue ? (faceDescription.Gender == Gender.Male ? _maleAdultString : _femaleAdultString) : _otherAdultString) + $" ({faceDescription.Age})";
                }
                else
                {
                    yield return (faceDescription.Gender.HasValue ? (faceDescription.Gender == Gender.Male ? _maleChildString : _femaleChildString) : _otherChildString) + $" ({faceDescription.Age})";
                }
            }
        }

        private string GetTranslatedGender(Gender? genderEnum, TranslationService translationService)
        {
            var gender = (genderEnum.HasValue ? (genderEnum == Gender.Male ? Gender.Male.ToString() : Gender.Female.ToString()) : "Person");

            if (_languageCode != null)
            {
                gender = translationService.TranslateText(new[] {gender}, _languageCode, TranslationLanguage.English).First();
            }

            return gender;
        }
    }
}