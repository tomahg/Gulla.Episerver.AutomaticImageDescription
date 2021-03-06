﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Gulla.Episerver.AutomaticImageDescription.Core.PropertyDefinitions;
using Gulla.Episerver.AutomaticImageDescription.Core.Translation;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;

namespace Gulla.Episerver.AutomaticImageDescription.Core.Image.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public abstract class BaseImageDetailsAttribute : Attribute
    {
        /// <summary>
        /// To be overridden in derived class. Flag if the Update method needs imageAnalyzerResult to be populated.
        /// </summary>
        public virtual bool AnalyzeImageContent => false;

        /// <summary>
        /// To be overridden in derived class. Flag if the Update method needs ocrResult to be populated.
        /// </summary>
        public virtual bool AnalyzeImageOcr => false;

        /// <summary>
        /// To be overridden in derived class. Flag if the Update method needs translationService to be populated.
        /// </summary>
        public virtual bool RequireTranslations => false;

        /// <summary>
        /// Updates the property that is decorated with this attribute. Call propertyInfo.SetValue(content, imageAnalyzerResult["DesiredValue"]).
        /// </summary>
        /// <param name="propertyAccess">Object with method used to SetValue of the property decorated with this attribute.</param>
        /// <param name="imageAnalyzerResult">ImageAnalyzer result.</param>
        /// <param name="ocrResult"> OCR result.</param>
        /// <param name="translationService">Service for translating text. Will be null if translation is not configured in app settings.</param>
        public abstract void Update(PropertyAccess propertyAccess, ImageAnalysis imageAnalyzerResult, OcrResult ocrResult, TranslationService translationService);

        protected static bool IsBooleanProperty(PropertyInfo propertyInfo)
        {
            return propertyInfo.PropertyType == typeof(bool);
        }

        protected static bool IsIntProperty(PropertyInfo propertyInfo)
        {
            return propertyInfo.PropertyType == typeof(int);
        }

        protected static bool IsDoubleProperty(PropertyInfo propertyInfo)
        {
            return propertyInfo.PropertyType == typeof(double);
        }

        protected static bool IsStringProperty(PropertyInfo propertyInfo)
        {
            return propertyInfo.PropertyType == typeof(string);
        }

        protected static bool IsLocalizedStringProperty(PropertyInfo propertyInfo)
        {
            return propertyInfo.PropertyType == typeof(IList<LocalizedString>);
        }

        protected static bool IsLocalizedStringListProperty(PropertyInfo propertyInfo)
        {
            return propertyInfo.PropertyType == typeof(IList<LocalizedStringList>);
        }

        protected static bool IsStringListProperty(PropertyInfo propertyInfo)
        {
            return propertyInfo.PropertyType == typeof(IList<string>) ||
                   propertyInfo.PropertyType == typeof(IEnumerable<string>) ||
                   propertyInfo.PropertyType == typeof(ICollection);
        }
    }
}