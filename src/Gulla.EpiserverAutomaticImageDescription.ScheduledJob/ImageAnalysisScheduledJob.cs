using EPiServer.PlugIn;
using EPiServer.Scheduler;

namespace Gulla.EpiserverAutomaticImageDescription.ScheduledJob
{
    [ScheduledPlugIn(DisplayName = "Analyze all images, and update metadata")]
    public class ImageAnalysisScheduledJob : ScheduledJobBase
    {
        private bool _stopSignaled;

        public ImageAnalysisScheduledJob()
        {
            IsStoppable = true;
        }

        public override string Execute()
        {
            OnStatusChanged("Analyzing images...");
            OnStatusChanged("Nah, fuck it!");
            OnStatusChanged("I quit!");
            OnStatusChanged("Finished!");

            if (_stopSignaled)
            {
                return "Job stopped!";
            }

            return "Job completed. No changes were made!";
        }

        public override void Stop()
        {
            _stopSignaled = true;
        }
    }
}
