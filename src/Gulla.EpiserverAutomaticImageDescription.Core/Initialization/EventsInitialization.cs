using EPiServer;
using EPiServer.Core;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;
using Gulla.EpiserverAutomaticImageDescription.Core.ImageAnalysis;

namespace Gulla.EpiserverAutomaticImageDescription.Core.Initialization
{
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
    public class EventsInitialization : IInitializableModule
    {
        public void Initialize(InitializationEngine context)
        {
            var events = ServiceLocator.Current.GetInstance<IContentEvents>();
            events.CreatingContent += CreatingContent;
        }

        private void CreatingContent(object sender, ContentEventArgs e)
        {
            if (e.Content is ImageData image)
            {
                ImageAnalyzer.AnalyzeImageAndUpdateMetaData(image);
            }
        }

        public void Uninitialize(InitializationEngine context)
        {

        }
    }
}