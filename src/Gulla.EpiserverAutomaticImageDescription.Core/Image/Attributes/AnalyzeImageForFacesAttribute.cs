﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Gulla.EpiserverAutomaticImageDescription.Core.Translation;
using Gulla.EpiserverAutomaticImageDescription.Core.Translation.Constants;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;

namespace Gulla.EpiserverAutomaticImageDescription.Core.Image.Attributes
{
    /// <summary>
    /// Analyze image for faces. Apply to string or IList&lt;string&gt; properties.
    /// </summary>
    public class AnalyzeImageForFacesAttribute : BaseImageDetailsAttribute
    {
        /// <summary>
        /// Analyze image for faces. Apply to string or IList&lt;string&gt; properties.
        /// </summary>
        /// <param name="languageCode">Translate tags to specified language.</param>
        public AnalyzeImageForFacesAttribute(string languageCode = null)
        {
            LanguageCode = languageCode;
        }

        private string LanguageCode { get; }

        public override bool AnalyzeImageContent => true;

        public override void Update(object content, ImageAnalysis imageAnalyzerResult, OcrResult ocrResult, PropertyInfo propertyInfo)
        {
            if (imageAnalyzerResult.Faces == null || imageAnalyzerResult.Faces.Count == 0)
            {
                return;
            }

            var faces = imageAnalyzerResult.Faces.Select(x =>  $"{GetTranslatedGender(x.Gender)} ({x.Age})"); 

            if (IsStringProperty(propertyInfo))
            {
                propertyInfo.SetValue(content, string.Join(", ", faces));
            }
            else if (IsStringListProperty(propertyInfo))
            {
                propertyInfo.SetValue(content, faces.ToList());
            }
        }

        private string GetTranslatedGender(Gender? genderEnum)
        {
            var gender = (genderEnum.HasValue ? (genderEnum == Gender.Male ? "Male" : "Female") : "Unknown");

            if (LanguageCode != null)
            {
                gender = Translator.TranslateText(new[] {gender}, LanguageCode, TranslationLanguage.English).Select(x => x.Translations).SelectMany(x => x).First().Text;
            }

            return gender;
        }
    }
}