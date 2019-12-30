using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Configuration;
using EPiServer.Core;
using Gulla.EpiserverAutomaticImageDescription.Core.Image.Attributes;
using Gulla.EpiserverAutomaticImageDescription.Core.Image.Models;
using Gulla.EpiserverAutomaticImageDescription.Core.Translation;
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
            var imagePropertiesWithAnalyzeAttributes = GetPropertiesWithAttribute(image, typeof(BaseImageDetailsAttribute)).ToList();
            if (!imagePropertiesWithAnalyzeAttributes.Any())
            {
                return;
            }

            ImageAnalysis imageAnalysisResult = null;
            OcrResult ocrResult = null;
            TranslationService translationService = null;

            var analyzeAttributes = GetAttributeContentPropertyList(imagePropertiesWithAnalyzeAttributes).ToList();
            if (analyzeAttributes.Any(x => x.Attribute.AnalyzeImageContent))
            {
                imageAnalysisResult = AnalyzeImage(GetImageStream(image));
            }
            if (analyzeAttributes.Any(x => x.Attribute.AnalyzeImageOcr))
            {
                ocrResult = OcrAnalyzeImage(GetImageStream(image));
            }
            if (analyzeAttributes.Any(x => x.Attribute.RequireTranslations))
            {
                // Creates authorization token + empty cache.
                translationService = TranslationService.GetInstanceIfConfigured();
                if (translationService == null)
                {
                    throw new ConfigurationErrorsException($"The attribute {analyzeAttributes.FirstOrDefault(x => x.Attribute.RequireTranslations)?.Attribute} requires translations to be configured but the required app settings is missing from web.config.");
                }
            }
            
            foreach (var attributeContentProperty in analyzeAttributes)
            {
                attributeContentProperty.Attribute.Update(attributeContentProperty.Content, imageAnalysisResult, ocrResult, attributeContentProperty.Property, translationService);
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

        private static IEnumerable<AttributeContentProperty> GetAttributeContentPropertyList(IEnumerable<ContentProperty> contentProperties)
        {
            foreach (var contentProperty in contentProperties)
            {
                var attribute = contentProperty.Property.GetCustomAttributes(typeof(BaseImageDetailsAttribute)).Cast<BaseImageDetailsAttribute>().FirstOrDefault();
                if (attribute != null)
                {
                    yield return new AttributeContentProperty()
                    {
                        Attribute = attribute,
                        Content = contentProperty.Content,
                        Property = contentProperty.Property
                    };
                }
            }
        }

        private static ImageAnalysis AnalyzeImage(Stream image)
        {
            var task = Task.Run(() => AnalyzeImageFeatures(image));
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

            var details = new List<Details>
            {
                Details.Celebrities,
                Details.Landmarks
            };

            return await Client.AnalyzeImageInStreamAsync(image, features, details);
        }

        private static OcrResult OcrAnalyzeImage(Stream image)
        {
            var task = Task.Run(() => OcrAnalyzeImageStream(image));
            return task.Result;
        }

        private static async Task<OcrResult> OcrAnalyzeImageStream(Stream image)
        {
            return await Client.RecognizePrintedTextInStreamAsync(true, image);
        }

        private static Stream GetImageStream(ImageData image)
        {
            return image.BinaryData.OpenRead();
        }

        private static ComputerVisionClient Client =>
            _client ?? (_client = new ComputerVisionClient(new ApiKeyServiceClientCredentials(ComputerVisionApiSubscriptionKey))
            {
                Endpoint = ComputerVisionEndpoint
            });
    }
}