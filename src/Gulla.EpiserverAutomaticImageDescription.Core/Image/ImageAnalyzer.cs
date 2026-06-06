using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Azure;
using Azure.AI.Vision.ImageAnalysis;
using EPiServer.Core;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using Gulla.Episerver.AutomaticImageDescription.Core.Image.Attributes;
using Gulla.Episerver.AutomaticImageDescription.Core.Image.Interface;
using Gulla.Episerver.AutomaticImageDescription.Core.Image.Models;
using Gulla.Episerver.AutomaticImageDescription.Core.Translation;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Gulla.Episerver.AutomaticImageDescription.Core.Image
{
    public static class ImageAnalyzer
    {
        private static readonly int MaxDimension = 4200;
        private static readonly int ScaleDownDimension = 1280;
        private static readonly long MaxFileSize = 4 * 1024 * 1024; // 4MB

        private static IOptions<AutomaticImageDescriptionOptions> _configuration;
        private static IOptions<AutomaticImageDescriptionOptions> Configuration => _configuration ??= ServiceLocator.Current.GetInstance<IOptions<AutomaticImageDescriptionOptions>>();

        private static readonly string ComputerVisionApiSubscriptionKey = Configuration.Value.ComputerVisionSubscriptionKey;
        private static readonly string ComputerVisionEndpoint = Configuration.Value.ComputerVisionEndpoint;
        private static readonly ILogger Log = LogManager.GetLogger();

        private static ComputerVisionClient _client;

        public static bool AnalyzeImageAndUpdateMetaData(ImageData imageData)
        {
            try
            {
                var imagePropertiesWithAnalyzeAttributes = GetPropertiesWithAttribute(imageData, typeof(BaseImageDetailsAttribute)).ToList();

                if (!imagePropertiesWithAnalyzeAttributes.Any() ||
                    !ImageIsOfSupportedFileSizeAndDimensions(imageData))
                {
                    MarkAnalysisAsCompleted(imageData);
                    return false;
                }

                Stream imageStream = null;
                if (ImageIsOfSupportedFormat(imageData))
                {
                    imageStream = GetImageStream(imageData);
                }
                else if (ImageIsOfSupportedFormatWithConversion(imageData))
                {
                    imageStream = GetImageStreamWithConversion(imageData);
                }
                else
                {
                    MarkAnalysisAsCompleted(imageData);
                    return false;
                }

                imageStream = ResizeImageStreamIfNeeded(imageStream);

                byte[] imageBytes;
                using (imageStream)
                {
                    imageBytes = ToByteArray(imageStream);
                }

                var analyzeAttributes = GetAttributeContentPropertyList(imagePropertiesWithAnalyzeAttributes).ToList();
                var imageAnalysisResult = GetImageAnalysisResultOrDefault(imageBytes, analyzeAttributes);
                var readResult = GetOcrResultOrDefault(imageBytes, analyzeAttributes);
                var translationService = GetTranslationServiceOrDefault(analyzeAttributes);

                foreach (var attributeContentProperty in analyzeAttributes)
                {
                    var propertyAccess = new PropertyAccess(imageData, attributeContentProperty.Content, attributeContentProperty.Property);
                    attributeContentProperty.Attribute.Update(propertyAccess, imageAnalysisResult, readResult, translationService);
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

            using var stream = imageBlob.OpenRead();

            // Image dimensions, min
            try
            {
                var image = SixLabors.ImageSharp.Image.Load(stream);
                if (image.Width < 50 || image.Height < 50)
                {
                    image.Dispose();
                    Log.Debug($"The image '{imageData.Name}' with content id '{imageData.ContentLink.ID}' is too small for image analysis (at least one dimension <50px)");
                    return false;
                }
                image.Dispose();
            }
            catch (Exception e)
            {
                Log.Error($"Error validating image '{imageData.Name}' with content id '{imageData.ContentLink.ID}'", e);
                return false;
            }

            return true;
        }

        private static bool ImageIsOfSupportedFormat(ImageData imageData)
        {
            return imageData.Name.ToLower().EndsWith(".jpg") || imageData.Name.ToLower().EndsWith(".jpeg") || imageData.Name.ToLower().EndsWith(".png") || imageData.Name.ToLower().EndsWith(".bmp");
        }

        private static bool ImageIsOfSupportedFormatWithConversion(ImageData imageData)
        {
            return imageData.Name.ToLower().EndsWith(".webp");
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
                    yield return new AttributeContentProperty
                    {
                        Attribute = attribute,
                        Content = contentProperty.Content,
                        Property = contentProperty.Property
                    };
                }
            }
        }

        private static ImageAnalysis GetImageAnalysisResultOrDefault(byte[] imageBytes, IEnumerable<AttributeContentProperty> attributes)
        {
            return attributes.Any(x => x.Attribute.AnalyzeImageContent) ? AnalyzeImage(imageBytes) : null;
        }

        private static ReadResult GetOcrResultOrDefault(byte[] imageBytes, IEnumerable<AttributeContentProperty> attributes)
        {
            return attributes.Any(x => x.Attribute.AnalyzeImageOcr) ? OcrAnalyzeImage(imageBytes) : null;
        }

        private static TranslationService GetTranslationServiceOrDefault(IEnumerable<AttributeContentProperty> attributes)
        {
            var attributeList = attributes.ToList();
            if (attributeList.Any(x => x.Attribute.RequireTranslations))
            {
                var translationService = TranslationService.GetInstanceIfConfigured();
                if (translationService == null)
                {
                    throw new Exception($"The attribute {attributeList.FirstOrDefault(x => x.Attribute.RequireTranslations)?.Attribute} requires translations to be configured but the required app settings is missing from web.config.");
                }

                return translationService;
            }

            return null;
        }

        private static ImageAnalysis AnalyzeImage(byte[] imageBytes)
        {
            // Each Azure SDK call gets its own stream over the buffered bytes; the SDK is free to
            // dispose the stream it is handed without affecting any other call.
            var task = Task.Run(() => AnalyzeImageFeatures(new MemoryStream(imageBytes)));
            return task.Result;
        }

        private static async Task<ImageAnalysis> AnalyzeImageFeatures(Stream image)
        {
            var features = new List<VisualFeatureTypes?>
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

            var details = new List<Details?>
            {
                Details.Landmarks
            };

            return await Client.AnalyzeImageInStreamAsync(image, features, details);
        }

        private static ReadResult OcrAnalyzeImage(byte[] imageBytes)
        {
            return ReadClient.Analyze(BinaryData.FromBytes(imageBytes), VisualFeatures.Read).Value.Read;
        }

        private static byte[] ToByteArray(Stream stream)
        {
            if (stream is MemoryStream memoryStream)
            {
                return memoryStream.ToArray();
            }

            using var buffer = new MemoryStream();
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            stream.CopyTo(buffer);
            return buffer.ToArray();
        }

        private static Stream GetImageStream(ImageData image)
        {
            // Buffer the blob into memory so downstream readers (ImageSharp, then OCR) can each
            // seek back and re-read. The Azure blob LazyLoadingReadOnlyStream is read only once here;
            // it does not reliably support being re-read after it has been consumed.
            var memoryStream = new MemoryStream();
            using (var blobStream = image.BinaryData.OpenRead())
            {
                blobStream.CopyTo(memoryStream);
            }

            memoryStream.Position = 0;
            return memoryStream;
        }


        private static Stream ResizeImageStreamIfNeeded(Stream imageStream)
        {
            var needsProcessing = false;

            if (imageStream.CanSeek && imageStream.Length > MaxFileSize)
            {
                needsProcessing = true;
            }

            using var imageSharpImage = SixLabors.ImageSharp.Image.Load(imageStream);

            if (imageSharpImage.Width > MaxDimension || imageSharpImage.Height > MaxDimension)
            {
                needsProcessing = true;

                var ratioX = (double)ScaleDownDimension / imageSharpImage.Width;
                var ratioY = (double)ScaleDownDimension / imageSharpImage.Height;
                var ratio = Math.Min(ratioX, ratioY);

                var newWidth = (int)(imageSharpImage.Width * ratio);
                var newHeight = (int)(imageSharpImage.Height * ratio);

                imageSharpImage.Mutate(x => x.Resize(newWidth, newHeight));
            }

            if (!needsProcessing)
            {
                if (imageStream.CanSeek)
                {
                    imageStream.Position = 0;
                    return imageStream;
                }
            }

            var outputStream = new MemoryStream();
            imageSharpImage.SaveAsPng(outputStream);

            while (outputStream.Length > MaxFileSize)
            {
                var newWidth = (int)(imageSharpImage.Width * 0.75);
                var newHeight = (int)(imageSharpImage.Height * 0.75);

                imageSharpImage.Mutate(x => x.Resize(newWidth, newHeight));
                outputStream.SetLength(0);
                imageSharpImage.SaveAsPng(outputStream);
            }

            imageStream.Dispose();
            outputStream.Position = 0;
            return outputStream;
        }

        private static MemoryStream GetImageStreamWithConversion(ImageData image)
        {
            using var imageSharpImage = SixLabors.ImageSharp.Image.Load(image.BinaryData.OpenRead());
            var outputStream = new MemoryStream();
            imageSharpImage.SaveAsPng(outputStream);
            outputStream.Position = 0; // Reset position to allow stream to be read again
            return outputStream;
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
            _client ??= new ComputerVisionClient(new ApiKeyServiceClientCredentials(ComputerVisionApiSubscriptionKey))
            {
                Endpoint = ComputerVisionEndpoint
            };

        private static ImageAnalysisClient _readClient;

        private static ImageAnalysisClient ReadClient =>
            _readClient ??= new ImageAnalysisClient(new Uri(ComputerVisionEndpoint), new AzureKeyCredential(ComputerVisionApiSubscriptionKey));
    }
}