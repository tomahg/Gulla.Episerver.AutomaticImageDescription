using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;
using Gulla.EpiserverAutomaticImageDescription.Core.Translation.Auth;
using Gulla.EpiserverAutomaticImageDescription.Core.Translation.DTO;
using Newtonsoft.Json;

namespace Gulla.EpiserverAutomaticImageDescription.Core.Translation
{
    public static class Translator
    {
        private static readonly string TranslatorSubscriptionKey = WebConfigurationManager.AppSettings["Gulla.EpiserverAutomaticImageDescription:Translator.SubscriptionKey"];
        private const string TranslatorEndpoint = "https://api.cognitive.microsofttranslator.com";

        public static IEnumerable<TranslationResult> TranslateText(IEnumerable<string> inputText, string toLanguage, string fromLanguage)
        {
            var auth = new AuthToken(TranslatorSubscriptionKey);
            var requestToken = await auth.GetAccessTokenAsync();

            var route = $"/translate?api-version=3.0&to={toLanguage}" + (fromLanguage != null ? $"&from={fromLanguage}" : "");
            var content = inputText.Select(x => new TextForTranslation { Text = x }).ToArray();
            var requestBody = JsonConvert.SerializeObject(content);

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;

                request.RequestUri = new Uri(TranslatorEndpoint + route);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                request.Headers.Add("Ocp-Apim-Subscription-Key", TranslatorSubscriptionKey);
                request.Headers.Add("Authorization", requestToken);

                var response = await client.SendAsync(request).ConfigureAwait(false);
                var result = await response.Content.ReadAsStringAsync();

                return JsonConvert.DeserializeObject<TranslationResult[]>(result);
            }
        }
    }
}