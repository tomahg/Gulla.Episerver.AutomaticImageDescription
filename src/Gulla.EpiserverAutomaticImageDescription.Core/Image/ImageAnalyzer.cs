﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Configuration;
using EPiServer.Core;
using Gulla.EpiserverAutomaticImageDescription.Core.Image.Attributes;
using Gulla.EpiserverAutomaticImageDescription.Core.Image.Models;
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

            var analyzeAttributes = imagePropertiesWithAnalyzeAttributes.Select(x => x.Property.GetCustomAttributes(typeof(BaseImageDetailsAttribute), true).Cast<BaseImageDetailsAttribute>()).SelectMany(x => x).ToList();
            if (analyzeAttributes.Any(x => x.AnalyzeImageContent))
            {
                imageAnalysisResult = AnalyzeImage(GetImageStream(image));
            }
            if (analyzeAttributes.Any(y => y.AnalyzeImageOcr))
            {
                ocrResult = OcrAnalyzeImage(GetImageStream(image));
            }

            foreach (var contentProperty in imagePropertiesWithAnalyzeAttributes)
            {
                var attributesForProperty = contentProperty.Property.GetCustomAttributes(typeof(BaseImageDetailsAttribute), true).Cast<BaseImageDetailsAttribute>();
                foreach (var attribute in attributesForProperty)
                {
                    attribute.Update(contentProperty.Content, imageAnalysisResult, ocrResult, contentProperty.Property);
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

            var details = new List<Details>
            {
                Details.Celebrities,
                Details.Landmarks
            };

            return await Client.AnalyzeImageInStreamAsync(image, features, details);
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