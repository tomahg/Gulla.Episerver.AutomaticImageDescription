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
    /// Analyze image for faces. Apply to bool, int, string, IList&lt;string&gt;, LocalizedString or LocalizedStringList properties.
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
        /// Analyze image for faces. Use generic names for gender. Apply to bool, int, string, IList&lt;string&gt;, LocalizedString or LocalizedStringList properties.
        /// </summary>
        /// <param name="languageCode">Translate tags to specified language.</param>
        public AnalyzeImageForFacesAttribute(string languageCode = null)
        {
            _languageCode = languageCode;
        }

        /// <summary>
        /// Analyze image for faces. Supply your own names for gender, child and adult, and cutoff age. Apply to bool, int, string, IList&lt;string&gt;, LocalizedString or LocalizedStringList properties.
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
                if (IsBooleanProperty(propertyAccess.Property))
                {
                    propertyAccess.SetValue(false);
                }
                else if (IsIntProperty(propertyAccess.Property))
                {
                    propertyAccess.SetValue(0);
                }
                return;
            }

            if (IsBooleanProperty(propertyAccess.Property))
            {
                propertyAccess.SetValue(imageAnalyzerResult.Faces.Any());
            }
            else if (IsIntProperty(propertyAccess.Property))
            {
                propertyAccess.SetValue(imageAnalyzerResult.Faces.Count);
            }
            else if (IsStringProperty(propertyAccess.Property))
            {
                propertyAccess.SetValue(string.Join(", ", GetTranslatedStrings(imageAnalyzerResult.Faces, translationService)));
            }
            else if (IsStringListProperty(propertyAccess.Property))
            {
                propertyAccess.SetValue(GetTranslatedStrings(imageAnalyzerResult.Faces, translationService).ToList());
            }
            else if (IsLocalizedStringProperty(propertyAccess.Property))
            {
                propertyAccess.SetValue(GetTranslatedLocalizedStrings(GetStrings(imageAnalyzerResult.Faces).ToList(), GetLanguageCodes(), translationService));
            }
            else if (IsLocalizedStringListProperty(propertyAccess.Property))
            {
                propertyAccess.SetValue(GetTranslatedLocalizedStringLists(GetStrings(imageAnalyzerResult.Faces).ToList(), GetLanguageCodes(), translationService));
            }
        }

        private IEnumerable<string> GetStrings(IEnumerable<FaceDescription> faces)
        {
            return _genderValuesSpecified ? GetFacesWithSpecifiedValues(faces) : faces.Select(x => $"{GetGenderString(x.Gender)} ({x.Age})");
        }

        private IEnumerable<string> GetTranslatedStrings(IEnumerable<FaceDescription> faces, TranslationService translationService)
        {
            return _genderValuesSpecified ? GetFacesWithSpecifiedValues(faces) : faces.Select(x => $"{GetTranslatedGender(x.Gender, _languageCode, translationService)} ({x.Age})");
        }

        private static IEnumerable<string> GetTranslatedStrings(IEnumerable<string> faces, string toLanguage, TranslationService translationService)
        {
            var faceList = faces.ToList();
            var translatedFaces =  translationService.TranslateText(faceList, toLanguage, TranslationLanguage.English).Select(x => x.ToLower());
            
            // When gender values are specified, match the case when translating
            if (char.IsUpper(faceList.First()[0]))
            {
                return translatedFaces.Select(x => x[0].ToString().ToUpper() + (x.Length > 1 ? x.Substring(1) : ""));
            }

            return translatedFaces;
        }

        private IEnumerable<string> GetFacesWithSpecifiedValues(IEnumerable<FaceDescription> faces)
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

        private static string GetGenderString(Gender? genderEnum)
        {
            return (genderEnum.HasValue ? (genderEnum == Gender.Male ? Gender.Male.ToString() : Gender.Female.ToString()) : "Person").ToLower();
        }

        private static string GetTranslatedGender(Gender? genderEnum, string languageCode, TranslationService translationService)
        {
            var gender = (genderEnum.HasValue ? (genderEnum == Gender.Male ? Gender.Male.ToString() : Gender.Female.ToString()) : "Person");

            if (languageCode != null && languageCode != TranslationLanguage.English)
            {
                gender = translationService.TranslateText(new[] {gender}, languageCode, TranslationLanguage.English).First().ToLower();
            }

            return gender.ToLower();
        }

        private static IEnumerable<LocalizedString> GetTranslatedLocalizedStrings(IList<string> faces, IEnumerable<string> languageCodes, TranslationService translationService)
        {
            return languageCodes.Select(languageCode => GetTranslatedLocalizedString(faces, languageCode, translationService)).ToList();
        }

        private static LocalizedString GetTranslatedLocalizedString(IEnumerable<string> faces, string languageCode, TranslationService translationService)
        {
            if (languageCode == TranslationLanguage.English)
            {
                return new LocalizedString { Language = TranslationLanguage.English, Value = string.Join(", ", faces) };
            }

            return new LocalizedString { Language = languageCode, Value = string.Join(", ", GetTranslatedStrings(faces, languageCode, translationService)) };
        }

        private static IEnumerable<LocalizedStringList> GetTranslatedLocalizedStringLists(IList<string> faces, IEnumerable<string> languageCodes, TranslationService translationService)
        {
            return languageCodes.Select(languageCode => GetTranslatedLocalizedStringList(faces, languageCode, translationService)).ToList();
        }

        private static LocalizedStringList GetTranslatedLocalizedStringList(IList<string> faces, string languageCode, TranslationService translationService)
        {
            if (languageCode == TranslationLanguage.English)
            {
                return new LocalizedStringList { Language = TranslationLanguage.English, Value = faces };
            }

            return new LocalizedStringList { Language = languageCode, Value = GetTranslatedStrings(faces, languageCode, translationService).ToList() };
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