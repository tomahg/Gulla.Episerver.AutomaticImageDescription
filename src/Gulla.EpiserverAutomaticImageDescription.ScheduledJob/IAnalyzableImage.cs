namespace Gulla.EpiserverAutomaticImageDescription.ScheduledJob
{
    public interface IAnalyzableImage
    {
        bool ImageAnalysisCompleted { get; set; }
    }
}
