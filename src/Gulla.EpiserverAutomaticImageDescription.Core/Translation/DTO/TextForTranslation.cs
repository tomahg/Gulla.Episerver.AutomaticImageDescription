using Newtonsoft.Json;

namespace Gulla.EpiserverAutomaticImageDescription.Core.Translation.DTO
{
    [JsonObject]
    public class TextForTranslation
    {
        public string Text { get; set; }
    }
}