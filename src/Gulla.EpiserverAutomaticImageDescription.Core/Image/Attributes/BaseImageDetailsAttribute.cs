using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;

namespace Gulla.EpiserverAutomaticImageDescription.Core.Image.Attributes
{
    public abstract class BaseImageDetailsAttribute : Attribute
    {
        public virtual bool AnalyzeImageContent => false;

        public virtual bool AnalyzeImageOcr => false;

        public abstract void Update(object content, ImageAnalysis imageAnalyzerResult, OcrResult ocrResult, PropertyInfo propertyInfo);

        protected static bool IsBooleanProperty(PropertyInfo propertyInfo)
        {
            return propertyInfo.PropertyType == typeof(bool);
        }

        protected static bool IsDoubleProperty(PropertyInfo propertyInfo)
        {
            return propertyInfo.PropertyType == typeof(double);
        }

        protected static bool IsStringProperty(PropertyInfo propertyInfo)
        {
            return propertyInfo.PropertyType == typeof(string);
        }

        protected static bool IsStringListProperty(PropertyInfo propertyInfo)
        {
            return propertyInfo.PropertyType == typeof(IList<string>) ||
                   propertyInfo.PropertyType == typeof(IEnumerable<string>) ||
                   propertyInfo.PropertyType == typeof(ICollection);
        }
    }
}