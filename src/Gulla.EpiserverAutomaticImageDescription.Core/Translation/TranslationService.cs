using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;
using Gulla.Episerver.AutomaticImageDescription.Core.Translation.Cache;
using Gulla.Episerver.AutomaticImageDescription.Core.Translation.DTO;
using Newtonsoft.Json;

namespace Gulla.Episerver.AutomaticImageDescription.Core.Translation
{
    public class TranslationService
    {
        private static readonly string TranslatorSubscriptionKey = WebConfigurationManager.AppSettings["Gulla.Episerver.AutomaticImageDescription:Translator.SubscriptionKey"];
        private const string TranslatorEndpoint = "https://api.cognitive.microsofttranslator.com";
        private readonly TranslationCache _cache;

        private TranslationService()
        {
            _cache = new TranslationCache();
        }

        public static TranslationService GetInstanceIfConfigured()
        {
            if (string.IsNullOrEmpty(TranslatorSubscriptionKey))
            {
                return null;
            }

            return new TranslationService();
        }
        
        public IEnumerable<string> TranslateText(IEnumerable<string> inputText, string toLanguage, string fromLanguage)
        {
            var originalText = inputText.ToArray();
            var cacheKey = $"{fromLanguage}-{toLanguage}-{string.Join("", originalText)}";
            if (_cache.TryGetValue(cacheKey, out var cachedTranslation))
            {
                return cachedTranslation;
            }

            var task = Task.Run(() => TranslateTextRequest(originalText, toLanguage, fromLanguage));

            var translations = task.Result.Select(x => x.Translations).SelectMany(x => x).Select(x => x.Text).ToList();
            _cache.Add(cacheKey, translations);
            return translations;
        }

        public IEnumerable<string> TranslateTextUncached(IEnumerable<string> inputText, string toLanguage, string fromLanguage)
        {
            var task = Task.Run(() => TranslateTextRequest(inputText.ToArray(), toLanguage, fromLanguage));
            return task.Result.Select(x => x.Translations).SelectMany(x => x).Select(x => x.Text).ToList();
        }

        private async Task<IEnumerable<TranslationResult>> TranslateTextRequest(string[] inputText, string toLanguage, string fromLanguage)
        {
            var route = $"/translate?api-version=3.0&to={toLanguage}" + (fromLanguage != null ? $"&from={fromLanguage}" : "");
            var content = inputText.Select(x => new TextForTranslation {Text = x}).ToArray();

            var requestBody = JsonConvert.SerializeObject(content);

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(TranslatorEndpoint + route);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                request.Headers.Add("Ocp-Apim-Subscription-Key", TranslatorSubscriptionKey);

                var response = await client.SendAsync(request).ConfigureAwait(false);
                var result = await response.Content.ReadAsStringAsync();

                return JsonConvert.DeserializeObject<TranslationResult[]>(result);
            }
        }
    }
}