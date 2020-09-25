using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web.Configuration;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAccess;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using Gulla.Episerver.AutomaticImageDescription.Core.Image;
using Gulla.Episerver.AutomaticImageDescription.Core.Image.Interface;

namespace Gulla.Episerver.AutomaticImageDescription.ScheduledJob
{
    [ScheduledPlugIn(DisplayName = "Analyze all images, update metadata")]
    public class ImageAnalysisScheduledJob : ScheduledJobBase
    {
        private readonly IContentRepository _contentRepository = ServiceLocator.Current.GetInstance<IContentRepository>();
        private IEnumerable<ImageData> _images;
        private int _analyzeCount;
        private bool _stopSignaled;
        private readonly int _requestsPerMinute = 20;

        public ImageAnalysisScheduledJob()
        {
            var appSetting = WebConfigurationManager.AppSettings["Gulla.Episerver.AutomaticImageDescription:ScheduledJob.MaxRequestsPerMinute"];
            if (appSetting != null)
            {
                int.TryParse(appSetting, out _requestsPerMinute);
            }

            IsStoppable = true;
        }

        public override string Execute()
        {
            OnStatusChanged("Collecting information about images...");
            _images = GetAllImages().Where(IsNotProcessed).ToList();
            _analyzeCount = 0;

            foreach (var image in _images)
            {
                if (_stopSignaled)
                {
                    return "Job stopped. " + GetStatus();
                }

                OnStatusChanged($"Analyzing image number {_analyzeCount + 1}...");
                if (UpdateImage(image))
                {
                    _analyzeCount++;
                }

                if (_requestsPerMinute > 0)
                {
                    Thread.Sleep(60 / _requestsPerMinute * 1000);
                }
            }

            OnStatusChanged("Finished!");

            return "Job completed. " + GetStatus();
        }

        private IEnumerable<ImageData> GetAllImages()
        {
            var contentImages = GetImages(EPiServer.Web.SiteDefinition.Current.ContentAssetsRoot);
            var globalImages = GetImages(EPiServer.Web.SiteDefinition.Current.GlobalAssetsRoot);
            return contentImages.Union(globalImages);
        }

        private IEnumerable<ImageData> GetImages(ContentReference root)
        {
            var fileReferences = _contentRepository.GetDescendents(root);
            foreach (var fileReference in fileReferences)
            {
                var image = _contentRepository.Get<IContent>(fileReference) as ImageData;
                if (image != null)
                {
                    yield return image;
                }
            }
        }

        private static bool IsNotProcessed(ImageData image)
        {
            var analyzableImage = image as IAnalyzableImage;
            return analyzableImage == null || !analyzableImage.ImageAnalysisCompleted;
        }

        private bool UpdateImage(ImageData image)
        {
            var writableImage = image.CreateWritableClone() as ImageData;
            var updated = ImageAnalyzer.AnalyzeImageAndUpdateMetaData(writableImage);
            _contentRepository.Save(writableImage, SaveAction.Patch, AccessLevel.NoAccess);
            return updated;
        }

        private string GetStatus()
        {
            return $"{_analyzeCount} of {_images.Count()} images were updated.";
        }

        public override void Stop()
        {
            _stopSignaled = true;
        }
    }
}
