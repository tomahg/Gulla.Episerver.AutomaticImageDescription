using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Threading.Tasks;
using System.Web.Configuration;
using EPiServer.Core;
using EPiServer.Framework.Blobs;
using Gulla.Episerver.AutomaticImageDescription.Core.Image.Attributes;
using Gulla.Episerver.AutomaticImageDescription.Core.Image.Interface;
using Gulla.Episerver.AutomaticImageDescription.Core.Image.Models;
using Gulla.Episerver.AutomaticImageDescription.Core.Translation;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;

namespace Gulla.Episerver.AutomaticImageDescription.Core.Image
{
    public static class ImageAnalyzer
    {
        private static readonly string ComputerVisionApiSubscriptionKey = WebConfigurationManager.AppSettings["Gulla.Episerver.AutomaticImageDescription:ComputerVision.SubscriptionKey"];
        private static readonly string ComputerVisionEndpoint = WebConfigurationManager.AppSettings["Gulla.Episerver.AutomaticImageDescription:ComputerVision.Endpoint"];

        private static ComputerVisionClient _client;

        public static void AnalyzeImageAndUpdateMetaData(ImageData image)
        {
            var imagePropertiesWithAnalyzeAttributes = GetPropertiesWithAttribute(image, typeof(BaseImageDetailsAttribute)).ToList();
            if (!imagePropertiesWithAnalyzeAttributes.Any())
            {
                return;
            }

            if (!ImageIsOfSupportedFormat(image) || !ImageIsOfSupportedFileSizeAndDimensions(image))
            {
                return;
            }

            var analyzeAttributes = GetAttributeContentPropertyList(imagePropertiesWithAnalyzeAttributes).ToList();
            var imageAnalysisResult = GetImageAnalysisResultOrDefault(image, analyzeAttributes);
            var ocrResult = GetOcrResultOrDefault(image, analyzeAttributes);
            var translationService = GetTranslationServiceOrDefault(analyzeAttributes);

            foreach (var attributeContentProperty in analyzeAttributes)
            {
                var propertyAccess = new PropertyAccess(image, attributeContentProperty.Content, attributeContentProperty.Property);
                attributeContentProperty.Attribute.Update(propertyAccess, imageAnalysisResult, ocrResult, translationService);
            }

            MarkAnalysisAsCompleted(image);
        }

        private static bool ImageIsOfSupportedFileSizeAndDimensions(ImageData imageData)
        {
            var imageBlob = imageData.BinaryData;

            using (var stream = imageBlob.OpenRead())
            {
                var image = System.Drawing.Image.FromStream(stream, false);

                if (image.Width < 50 || image.Height < 50 || image.Width > 4200 || image.Height > 4200)
                {
                    image.Dispose();
                    return false;
                }

                var path = ((FileBlob)imageBlob).FilePath;
                var numBytes = new FileInfo(path).Length;

                if (numBytes > 4 * 1024 * 1024)
                {
                    image.Dispose();
                    return false;
                }

                image.Dispose();
            }

            return true;
        }

        private static bool ImageIsOfSupportedFormat(ImageData imageData)
        {
            return imageData.Name.ToLower().EndsWith(".jpg") || imageData.Name.ToLower().EndsWith(".jpeg") || imageData.Name.ToLower().EndsWith(".png") || imageData.Name.ToLower().EndsWith(".bmp");
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

        private static ImageAnalysis GetImageAnalysisResultOrDefault(ImageData image, IEnumerable<AttributeContentProperty> attributes)
        {
            return attributes.Any(x => x.Attribute.AnalyzeImageContent) ? AnalyzeImage(GetImageStream(image)) : null;
        }

        private static OcrResult GetOcrResultOrDefault(ImageData image, IEnumerable<AttributeContentProperty> attributes)
        {
            return attributes.Any(x => x.Attribute.AnalyzeImageOcr) ? OcrAnalyzeImage(GetImageStream(image)) : null;
        }

        private static TranslationService GetTranslationServiceOrDefault(IEnumerable<AttributeContentProperty> attributes)
        {
            var attributeList = attributes.ToList();
            if (attributeList.Any(x => x.Attribute.RequireTranslations))
            {
                var translationService = TranslationService.GetInstanceIfConfigured();
                if (translationService == null)
                {
                    throw new ConfigurationErrorsException($"The attribute {attributeList.FirstOrDefault(x => x.Attribute.RequireTranslations)?.Attribute} requires translations to be configured but the required app settings is missing from web.config.");
                }

                return translationService;
            }

            return null;
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

        private static void MarkAnalysisAsCompleted(ImageData image)
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            if (image is IAnalyzableImage analyzableImage)
            {
                analyzableImage.ImageAnalysisCompleted = true;
            }
        }

        private static ComputerVisionClient Client =>
            _client ?? (_client = new ComputerVisionClient(new ApiKeyServiceClientCredentials(ComputerVisionApiSubscriptionKey))
            {
                Endpoint = ComputerVisionEndpoint
            });
    }
}