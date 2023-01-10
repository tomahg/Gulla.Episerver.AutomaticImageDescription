using EPiServer.DataAbstraction;

namespace Gulla.Episerver.AutomaticImageDescription.Core
{
    public class AutomaticImageDescriptionOptions
    {
        public string ComputerVisionSubscriptionKey { get; set; }
        public string ComputerVisionEndpoint { get; set; }
        public string TranslatorSubscriptionKey { get; set; }
        public string TranslatorSubscriptionRegion { get; set; }
        public int ScheduledJobMaxRequestsPerMinute { get; set; }
    }
}
