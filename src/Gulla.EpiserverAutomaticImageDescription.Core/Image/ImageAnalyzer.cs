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
using Gulla.EpiserverAutomaticImageDescription.Core.Image.Attributes;
using Gulla.EpiserverAutomaticImageDescription.Core.Image.Models;
using Gulla.EpiserverAutomaticImageDescription.Core.Translation;
using Gulla.EpiserverAutomaticImageDescription.Core.Translation.Constants;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;

namespace Gulla.EpiserverAutomaticImageDescription.Core.Image
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
                    UpdateDescription(imageAnalyzerResult, descriptionProperties);
                    UpdateTags(imageAnalyzerResult, tagProperties);
                    UpdateAdult(imageAnalyzerResult, adultProperties);
                    UpdateRacism(imageAnalyzerResult, racismProperties);
                    UpdateBrands(imageAnalyzerResult, brandsProperties);
                }
            }

            var ocrProperties = GetPropertiesWithAttribute(image, typeof(AnalyzeImageForOcrAttribute)).ToList();
            if (ocrProperties.Any())
            {
                var ocrResult = OcrAnalyzeImage(GetImageStream(image));
                if (ocrResult != null)
                {
                    UpdateOcr(ocrResult, ocrProperties);
                }
            }
        }

        private static IEnumerable<ContentProperty> GetPropertiesWithAttribute(IContent content, Type attribute)
        {
            var pageProperties = GetPagePropertiesWithAttribute(content, attribute);
            var blockProperties = GetBlockPropertiesWithAttribute(content, attribute);
            return pageProperties.Union(blockProperties);
        }

        private static IEnumerable<ContentProperty> GetPagePropertiesWithAttribute(IContent content, Type attribute)
        {
            return content.GetType().GetProperties()
                .Where(pageProperty => Attribute.IsDefined(pageProperty, attribute))
                .Select(property => new ContentProperty { Content = content, Property = property });
        }

        private static IEnumerable<ContentProperty> GetBlockPropertiesWithAttribute(IContent content, Type attribute)
        {
            return content.GetType().GetProperties()
                .Where(pageProperty => typeof(BlockData).IsAssignableFrom(pageProperty.PropertyType))
                .Select(propertyInfo => GetBlockPropertiesWithAttributeForSingleBlock(content, propertyInfo, attribute)).SelectMany(x => x);
        }

        private static IEnumerable<ContentProperty> GetBlockPropertiesWithAttributeForSingleBlock(IContent content, PropertyInfo localBlockProperty, Type attribute)
        {
            var blockPropertiesWithAttribute = localBlockProperty.PropertyType.GetProperties().Where(blockProperty => Attribute.IsDefined(blockProperty, attribute));
            var block = content.Property[localBlockProperty.Name].GetType().GetProperties().Single(x => x.Name == "Block").GetValue(content.Property[localBlockProperty.Name]);
            return blockPropertiesWithAttribute.Select(property => new ContentProperty { Content = block, Property = property });
        }

        private static ImageAnalysis AnalyzeImage(Stream image)
        {
            var task = Task.Run(async () => await AnalyzeImageFeatures(image));
            return task.Result;
        }

        private static async Task<ImageAnalysis> AnalyzeImageFeatures(Stream image)
        {
            var features = new List<VisualFeatureTypes>
            {
                VisualFeatureTypes.Adult,
                VisualFeatureTypes.Brands,
                VisualFeatureTypes.Categories,
                VisualFeatureTypes.Color,
                VisualFeatureTypes.Description,
                VisualFeatureTypes.Faces,
                VisualFeatureTypes.ImageType,
                VisualFeatureTypes.Objects,
                VisualFeatureTypes.Tags
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

        private static void UpdateDescription(ImageAnalysis imageAnalyzerResult, IEnumerable<ContentProperty> contentProperties)
        {
            if (imageAnalyzerResult?.Description?.Captions == null)
            {
                return;
            }

            foreach (var contentProperty in contentProperties)
            {
                if (IsStringProperty(contentProperty.Property))
                {
                    var descriptionTranslated = GetTranslatedDescription(imageAnalyzerResult.Description.Captions.Select(caption => caption.Text).FirstOrDefault(), contentProperty.Property);
                    contentProperty.Property.SetValue(contentProperty.Content, descriptionTranslated);
                }
            }
        }

        private static void UpdateTags(ImageAnalysis imageAnalyzerResult, IEnumerable<ContentProperty> contentProperties)
        {
            if (imageAnalyzerResult.Description.Tags == null || imageAnalyzerResult.Description.Tags.Count == 0)
            {
                return;
            }

            foreach (var contentProperty in contentProperties)
            {
                var tagsTranslated = GetTranslatedTags(imageAnalyzerResult.Description.Tags, contentProperty.Property);

                if (IsStringProperty(contentProperty.Property))
                {
                    contentProperty.Property.SetValue(contentProperty.Content, string.Join(", ", tagsTranslated));
                }
                else if (IsStringListProperty(contentProperty.Property))
                {
                    contentProperty.Property.SetValue(contentProperty.Content, tagsTranslated.ToList());
                }
            }
        }

        private static void UpdateAdult(ImageAnalysis imageAnalyzerResult, IEnumerable<ContentProperty> contentProperties)
        {
            if (imageAnalyzerResult.Adult == null)
            {
                return;
            }

            foreach (var contentProperty in contentProperties)
            {
                if (IsBooleanProperty(contentProperty.Property))
                {
                    contentProperty.Property.SetValue(contentProperty.Content, imageAnalyzerResult.Adult.IsAdultContent);
                }
                else if (IsDoubleProperty(contentProperty.Property))
                {
                    contentProperty.Property.SetValue(contentProperty.Content, imageAnalyzerResult.Adult.AdultScore);
                }
                else if (IsStringProperty(contentProperty.Property))
                {
                    contentProperty.Property.SetValue(contentProperty.Content, imageAnalyzerResult.Adult.AdultScore.ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        private static void UpdateRacism(ImageAnalysis imageAnalyzerResult, IEnumerable<ContentProperty> contentProperties)
        {
            if (imageAnalyzerResult.Adult == null)
            {
                return;
            }

            foreach (var contentProperty in contentProperties)
            {
                if (IsBooleanProperty(contentProperty.Property))
                {
                    contentProperty.Property.SetValue(contentProperty.Content, imageAnalyzerResult.Adult.IsRacyContent);
                }
                else if (IsDoubleProperty(contentProperty.Property))
                {
                    contentProperty.Property.SetValue(contentProperty.Content, imageAnalyzerResult.Adult.RacyScore);
                }
                else if (IsStringProperty(contentProperty.Property))
                {
                    contentProperty.Property.SetValue(contentProperty.Content, imageAnalyzerResult.Adult.RacyScore.ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        private static void UpdateBrands(ImageAnalysis imageAnalyzerResult, IEnumerable<ContentProperty> contentProperties)
        {
            if (imageAnalyzerResult.Brands == null || imageAnalyzerResult.Brands.Count == 0)
            {
                return;
            }

            foreach (var contentProperty in contentProperties)
            {
                if (IsStringProperty(contentProperty.Property))
                {
                    contentProperty.Property.SetValue(contentProperty.Content, string.Join(", ", imageAnalyzerResult.Brands.Select(x => x.Name)));
                }
                else if (IsStringListProperty(contentProperty.Property))
                {
                    contentProperty.Property.SetValue(contentProperty.Content, imageAnalyzerResult.Brands.Select(x => x.Name).ToList());
                }
            }
        }

        private static void UpdateOcr(OcrResult ocrResult, IEnumerable<ContentProperty> contentProperties)
        {
            if (ocrResult.Regions == null || ocrResult.Regions.Count == 0)
            {
                return;
            }

            foreach (var contentProperty in contentProperties)
            {
                var ocrTranslated = GetTranslatedOcr(ocrResult, contentProperty.Property);

                if (IsStringProperty(contentProperty.Property))
                {
                    contentProperty.Property.SetValue(contentProperty.Content, string.Join(" ", ocrTranslated));
                }
            }
        }

        private static string GetTranslatedDescription(string description, PropertyInfo propertyInfo)
        {
            var descriptionAttribute = propertyInfo.GetCustomAttribute<AnalyzeImageForDescriptionAttribute>();

            if (descriptionAttribute.LanguageCode != null)
            {
                description = Translator.TranslateText(new[] { description }, descriptionAttribute.LanguageCode, TranslationLanguage.English).First().Translations.First().Text;
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

            return Translator.TranslateText(tags, languageCode, TranslationLanguage.English).Select(x => x.Translations).Select(x => x.First().Text);
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