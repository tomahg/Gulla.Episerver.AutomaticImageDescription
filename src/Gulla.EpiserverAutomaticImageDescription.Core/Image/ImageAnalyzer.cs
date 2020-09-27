using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Configuration;
using EPiServer.Core;
using EPiServer.Logging;
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
        private static readonly ILogger Log = LogManager.GetLogger();

        private static ComputerVisionClient _client;

        public static bool AnalyzeImageAndUpdateMetaData(ImageData imageData)
        {
            try
            {
                var imagePropertiesWithAnalyzeAttributes = GetPropertiesWithAttribute(imageData, typeof(BaseImageDetailsAttribute)).ToList();

                if (!imagePropertiesWithAnalyzeAttributes.Any() ||
                    !ImageIsOfSupportedFormat(imageData) ||
                    !ImageIsOfSupportedFileSizeAndDimensions(imageData))
                {
                    MarkAnalysisAsCompleted(imageData);
                    return false;
                }

                var analyzeAttributes = GetAttributeContentPropertyList(imagePropertiesWithAnalyzeAttributes).ToList();
                var imageAnalysisResult = GetImageAnalysisResultOrDefault(imageData, analyzeAttributes);
                var ocrResult = GetOcrResultOrDefault(imageData, analyzeAttributes);
                var translationService = GetTranslationServiceOrDefault(analyzeAttributes);

                foreach (var attributeContentProperty in analyzeAttributes)
                {
                    var propertyAccess = new PropertyAccess(imageData, attributeContentProperty.Content, attributeContentProperty.Property);
                    attributeContentProperty.Attribute.Update(propertyAccess, imageAnalysisResult, ocrResult, translationService);
                }
            }
            catch (Exception e)
            {
                Log.Error($"Error analyzing image '{imageData.Name}' with content id '{imageData.ContentLink.ID}'", e);
            }

            MarkAnalysisAsCompleted(imageData);
            return true;
        }

        private static bool ImageIsOfSupportedFileSizeAndDimensions(ImageData imageData)
        {
            var imageBlob = imageData.BinaryData;

            using (var stream = imageBlob.OpenRead())
            {
                // Max file size: 4MB
                if (stream.Length > 4 * 1024 * 1024)
                {
                    Log.Debg($"The image '{imageData.Name}' with content id '{imageData.ContentLink.ID}' is too large for image analysis (>4MB)");
                    return false;
                }

                // Image dimensions, min/max
                try
                {
                    var image = System.Drawing.Image.FromStream(stream);
                    if (image.Width < 50 || image.Height < 50)
                    {
                        image.Dispose();
                        Log.Debg($"The image '{imageData.Name}' with content id '{imageData.ContentLink.ID}' is too small for image analysis (at least one dimension <50px)");
                        return false;
                    }
                    if (image.Width > 4200 || image.Height > 4200)
                    {
                        image.Dispose();
                        Log.Debg($"The image '{imageData.Name}' with content id '{imageData.ContentLink.ID}' is too large for image analysis (at least one dimension >4200px)");
                        return false;
                    }
                    image.Dispose();
                }
                catch (Exception e)
                {
                    Log.Error($"Error validating image '{imageData.Name}' with content id '{imageData.ContentLink.ID}'", e);
                    return false;
                }
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