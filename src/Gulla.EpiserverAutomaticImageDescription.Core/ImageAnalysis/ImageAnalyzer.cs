using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Configuration;
using EPiServer.Core;
using Gulla.EpiserverAutomaticImageDescription.Core.ImageAnalysis.Attributes;
using Gulla.EpiserverAutomaticImageDescription.Core.Translation;
using Gulla.EpiserverAutomaticImageDescription.Core.Translation.Constants;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;

namespace Gulla.EpiserverAutomaticImageDescription.Core.ImageAnalysis
{
    public static class ImageAnalyzer
    {
        private static readonly string ComputerVisionApiSubscriptionKey = WebConfigurationManager.AppSettings["Gulla.EpiserverAutomaticImageDescription:ComputerVision.SubscriptionKey"];
        private static readonly string ComputerVisionEndpoint = WebConfigurationManager.AppSettings["Gulla.EpiserverAutomaticImageDescription:ComputerVision.Endpoint"];

        private static ComputerVisionClient _client;

        public static void AnalyzeImageAndUpdateMetaData(ImageData image)
        {
            var descriptionProperties = GetPropertiesWithAttribute(image, typeof(AnalyzeImageForDescriptionAttribute)).ToList();
            var tagProperties = GetPropertiesWithAttribute(image, typeof(AnalyzeImageForTagsAttribute)).ToList();
            var adultProperties = GetPropertiesWithAttribute(image, typeof(AnalyzeImageForAdultContentAttribute)).ToList();
            var racismProperties = GetPropertiesWithAttribute(image, typeof(AnalyzeImageForRacismAttribute)).ToList();
            var brandsProperties = GetPropertiesWithAttribute(image, typeof(AnalyzeImageForBrandsAttribute)).ToList();

            if (descriptionProperties.Any() || tagProperties.Any() || adultProperties.Any() || racismProperties.Any() || brandsProperties.Any())
            {
                var imageAnalyzerResult = AnalyzeImage(GetImageStream(image));
                if (imageAnalyzerResult != null)
                {
                    UpdateDescription(image, imageAnalyzerResult, descriptionProperties);
                    UpdateTags(image, imageAnalyzerResult, tagProperties);
                    UpdateAdult(image, imageAnalyzerResult, adultProperties);
                    UpdateRacism(image, imageAnalyzerResult, racismProperties);
                    UpdateBrands(image, imageAnalyzerResult, brandsProperties);
                }
            }

            var ocrProperties = GetPropertiesWithAttribute(image, typeof(AnalyzeImageForOcrAttribute)).ToList();
            if (ocrProperties.Any())
            {
                var ocrResult = OcrAnalyzeImage(GetImageStream(image));
                if (ocrResult != null)
                {
                    UpdateOcr(image, ocrResult, ocrProperties);
                }
            }
        }

        private static IEnumerable<PropertyInfo> GetPropertiesWithAttribute(IContent content, Type attribute)
        {
            return content.GetType().GetProperties().Where(property => Attribute.IsDefined(property, attribute));
        }

        private static Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models.ImageAnalysis AnalyzeImage(Stream image)
        {
            var task = Task.Run(async () => await AnalyzeImageFeatures(image));
            return task.Result;
        }

        private static async Task<Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models.ImageAnalysis> AnalyzeImageFeatures(Stream image)
        {
            var features = new List<VisualFeatureTypes>
            {
                VisualFeatureTypes.Adult,
                VisualFeatureTypes.Brands,
                VisualFeatureTypes.Description
            };

            return await Client.AnalyzeImageInStreamAsync(image, features);
        }

        private static OcrResult OcrAnalyzeImage(Stream image)
        {
            var task = Task.Run(async () => await OcrAnalyzeImageStream(image));
            return task.Result;
        }

        private static async Task<OcrResult> OcrAnalyzeImageStream(Stream image)
        {
            return await Client.RecognizePrintedTextInStreamAsync(true, image);
        }

        private static void UpdateDescription(ImageData image, Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models.ImageAnalysis imageAnalyzerResult, IEnumerable<PropertyInfo> descriptionProperties)
        {
            if (imageAnalyzerResult?.Description?.Captions == null)
            {
                return;
            }

            foreach (var descriptionProperty in descriptionProperties)
            {
                if (IsStringProperty(descriptionProperty))
                {
                    var descriptionTranslated = GetTranslatedDescription(imageAnalyzerResult.Description.Captions.Select(caption => caption.Text).FirstOrDefault(), descriptionProperty);
                    descriptionProperty.SetValue(image, descriptionTranslated);
                }
            }
        }

        private static void UpdateTags(ImageData image, Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models.ImageAnalysis imageAnalyzerResult, IEnumerable<PropertyInfo> tagProperties)
        {
            if (imageAnalyzerResult.Description.Tags == null || imageAnalyzerResult.Description.Tags.Count == 0)
            {
                return;
            }

            foreach (var tagProperty in tagProperties)
            {
                var tagsTranslated = GetTranslatedTags(imageAnalyzerResult.Description.Tags, tagProperty);

                if (IsStringProperty(tagProperty))
                {
                    tagProperty.SetValue(image, string.Join(", ", tagsTranslated));
                }
                else if (IsStringListProperty(tagProperty))
                {
                    tagProperty.SetValue(image, tagsTranslated.ToList());
                }
            }
        }

        private static void UpdateAdult(ImageData image, Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models.ImageAnalysis imageAnalyzerResult, IEnumerable<PropertyInfo> adultProperties)
        {
            if (imageAnalyzerResult.Adult == null)
            {
                return;
            }

            foreach (var adultProperty in adultProperties)
            {
                if (IsBooleanProperty(adultProperty))
                {
                    adultProperty.SetValue(image, imageAnalyzerResult.Adult.IsAdultContent);
                }
                else if (IsDoubleProperty(adultProperty))
                {
                    adultProperty.SetValue(image, imageAnalyzerResult.Adult.AdultScore);
                }
                else if (IsStringProperty(adultProperty))
                {
                    adultProperty.SetValue(image, imageAnalyzerResult.Adult.AdultScore.ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        private static void UpdateRacism(ImageData image, Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models.ImageAnalysis imageAnalyzerResult, IEnumerable<PropertyInfo> racismProperties)
        {
            if (imageAnalyzerResult.Adult == null)
            {
                return;
            }

            foreach (var adultProperty in racismProperties)
            {
                if (IsBooleanProperty(adultProperty))
                {
                    adultProperty.SetValue(image, imageAnalyzerResult.Adult.IsRacyContent);
                }
                else if (IsDoubleProperty(adultProperty))
                {
                    adultProperty.SetValue(image, imageAnalyzerResult.Adult.RacyScore);
                }
                else if (IsStringProperty(adultProperty))
                {
                    adultProperty.SetValue(image, imageAnalyzerResult.Adult.RacyScore.ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        private static void UpdateBrands(ImageData image, Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models.ImageAnalysis imageAnalyzerResult, IEnumerable<PropertyInfo> brandsProperties)
        {
            if (imageAnalyzerResult.Brands == null || imageAnalyzerResult.Brands.Count == 0)
            {
                return;
            }

            foreach (var brandsProperty in brandsProperties)
            {
                if (IsStringProperty(brandsProperty))
                {
                    brandsProperty.SetValue(image, string.Join(", ", imageAnalyzerResult.Brands.Select(x => x.Name)));
                }
                else if (IsStringListProperty(brandsProperty))
                {
                    brandsProperty.SetValue(image, imageAnalyzerResult.Brands.Select(x => x.Name).ToList());
                }
            }
        }

        private static void UpdateOcr(ImageData image, OcrResult ocrResult, IEnumerable<PropertyInfo> ocrProperties)
        {
            if (ocrResult.Regions == null || ocrResult.Regions.Count == 0)
            {
                return;
            }

            foreach (var ocrProperty in ocrProperties)
            {
                var ocrTranslated = GetTranslatedOcr(ocrResult, ocrProperty);

                if (IsStringProperty(ocrProperty))
                {
                    
                    ocrProperty.SetValue(image,string.Join(" ", ocrTranslated));
                }
            }
        }

        private static string GetTranslatedDescription(string description, PropertyInfo propertyInfo)
        {
            var descriptionAttribute = propertyInfo.GetCustomAttribute<AnalyzeImageForDescriptionAttribute>();

            if (descriptionAttribute.LanguageCode != null)
            {
                description = Translator.TranslateText(new[] { description }, descriptionAttribute.LanguageCode, Language.English).First().Translations.First().Text;
            }

            return FormatDescription(description, descriptionAttribute.UpperCaseFirstLetter, descriptionAttribute.EndWithDot);
        }

        private static IEnumerable<string> GetTranslatedTags(IEnumerable<string> tags, PropertyInfo propertyInfo)
        {
            var languageCode = propertyInfo.GetCustomAttribute<AnalyzeImageForTagsAttribute>().LanguageCode;
            if (languageCode == null)
            {
                return tags;
            }

            return Translator.TranslateText(tags, languageCode, Language.English).Select(x => x.Translations).Select(x => x.First().Text);
        }

        private static IEnumerable<string> GetTranslatedOcr(OcrResult ocrResult, PropertyInfo propertyInfo)
        {
            var words = ocrResult.Regions.Select(x => x.Lines).SelectMany(x => x).Select(x => x.Words).SelectMany(x => x).Select(x => x.Text);
            var toLanguage = propertyInfo.GetCustomAttribute<AnalyzeImageForOcrAttribute>().LanguageCode;
            if (toLanguage == null)
            {
                return words;
            }

            var fromLanguage = propertyInfo.GetCustomAttribute<AnalyzeImageForOcrAttribute>().FromLanguageCode;
            return Translator.TranslateText(words, toLanguage, fromLanguage).Select(x => x.Translations).Select(x => x.First().Text);
        }

        private static string FormatDescription(string description, bool upperCaseFirstLetter, bool endWithDot)
        {
            if (string.IsNullOrEmpty(description))
            {
                return description;
            }

            if (upperCaseFirstLetter)
            {
                description = description[0].ToString().ToUpper() + (description.Length > 1 ? description.Substring(1) : "");
            }

            return description + (endWithDot && !description.Trim().EndsWith(".") ? "." : "");
        }

        private static Stream GetImageStream(ImageData image)
        {
            return image.BinaryData.OpenRead();
        }

        private static bool IsBooleanProperty(PropertyInfo propertyInfo)
        {
            return propertyInfo.PropertyType == typeof(bool);
        }

        private static bool IsDoubleProperty(PropertyInfo propertyInfo)
        {
            return propertyInfo.PropertyType == typeof(double);
        }

        private static bool IsStringProperty(PropertyInfo propertyInfo)
        {
            return propertyInfo.PropertyType == typeof(string);
        }

        private static bool IsStringListProperty(PropertyInfo propertyInfo)
        {
            return propertyInfo.PropertyType == typeof(IList<string>) ||
                   propertyInfo.PropertyType == typeof(IEnumerable<string>) ||
                   propertyInfo.PropertyType == typeof(ICollection);
        }

        private static ComputerVisionClient Client =>
            _client ?? (_client = new ComputerVisionClient(new ApiKeyServiceClientCredentials(ComputerVisionApiSubscriptionKey))
            {
                Endpoint = ComputerVisionEndpoint
            });
    }
}