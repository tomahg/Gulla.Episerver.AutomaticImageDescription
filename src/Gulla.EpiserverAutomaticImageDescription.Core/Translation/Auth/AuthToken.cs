using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Gulla.Episerver.AutomaticImageDescription.Core.Translation.Auth
{
    /// <summary>
    /// Client to call Cognitive Services Azure Auth Token service in order to get an access token.
    /// Exposes asynchronous as well as synchronous methods.
    /// </summary>
    public class AuthToken
    {
        /// URL of the token service
        private static Uri _serviceUrl;
        /// Name of header used to pass the subscription key to the token service
        private const string OcpApimSubscriptionKeyHeader = "Ocp-Apim-Subscription-Key";
        /// After obtaining a valid token, this class will cache it for this duration.
        /// Use a duration of 5 minutes, which is less than the actual token lifetime of 10 minutes.
        private static readonly TimeSpan TokenCacheDuration = new TimeSpan(0, 5, 0);

        /// Cache the value of the last valid token obtained from the token service.
        private string _storedTokenValue = string.Empty;
        /// When the last valid token was obtained.
        private DateTime _storedTokenTime = DateTime.MinValue;

        /// Gets the subscription key.
        public string SubscriptionKey { get; private set; } = string.Empty;

        /// Gets the HTTP status code for the most recent request to the token service.
        public HttpStatusCode RequestStatusCode { get; private set; }

        /// <summary>
        /// Creates a client to obtain an access token.
        /// </summary>
        /// <param name="endpoint">URL to token service.</param>
        /// <param name="key">Subscription key to use to get an authentication token.</param>
        public AuthToken(string endpoint, string key)
        {
            if (string.IsNullOrEmpty(endpoint))
            {
                throw new ArgumentNullException(nameof(endpoint), "An endpoint to token service is required");
            }

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key), "A subscription key is required");
            }

            SubscriptionKey = key;
            _serviceUrl = new Uri(endpoint);
            RequestStatusCode = HttpStatusCode.InternalServerError;
        }

        /// <summary>
        /// Gets a token for the specified subscription.
        /// </summary>
        /// <returns>The encoded JWT token prefixed with the string "Bearer ".</returns>
        /// <remarks>
        /// This method uses a cache to limit the number of request to the token service.
        /// A fresh token can be re-used during its lifetime of 10 minutes. After a successful
        /// request to the token service, this method caches the access token. Subsequent 
        /// invocations of the method return the cached token for the next 5 minutes. After
        /// 5 minutes, a new token is fetched from the token service and the cache is updated.
        /// </remarks>
        public async Task<string> GetAccessTokenAsync()
        {
            if (SubscriptionKey == string.Empty) return string.Empty;

            // Re-use the cached token if there is one.
            if ((DateTime.Now - _storedTokenTime) < TokenCacheDuration)
            {
                return _storedTokenValue;
            }

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = _serviceUrl;
                request.Content = new StringContent(string.Empty);
                request.Headers.TryAddWithoutValidation(OcpApimSubscriptionKeyHeader, SubscriptionKey);
                client.Timeout = TimeSpan.FromSeconds(2);
                var response = await client.SendAsync(request);
                RequestStatusCode = response.StatusCode;
                response.EnsureSuccessStatusCode();
                var token = await response.Content.ReadAsStringAsync();
                _storedTokenTime = DateTime.Now;
                _storedTokenValue = "Bearer " + token;
                return _storedTokenValue;
            }
        }

        /// <summary>
        /// Gets a token for the specified subscription. Synchronous version.
        /// Use of async version preferred
        /// </summary>
        /// <returns>The encoded JWT token prefixed with the string "Bearer ".</returns>
        /// <remarks>
        /// This method uses a cache to limit the number of request to the token service.
        /// A fresh token can be re-used during its lifetime of 10 minutes. After a successful
        /// request to the token service, this method caches the access token. Subsequent 
        /// invocations of the method return the cached token for the next 5 minutes. After
        /// 5 minutes, a new token is fetched from the token service and the cache is updated.
        /// </remarks>
        public string GetAccessToken()
        {
            // Re-use the cached token if there is one.
            if ((DateTime.Now - _storedTokenTime) < TokenCacheDuration)
            {
                return _storedTokenValue;
            }

            string accessToken = null;
            var task = Task.Run(async () =>
            {
                accessToken = await GetAccessTokenAsync();
            });

            while (!task.IsCompleted)
            {
                Thread.Yield();
            }
            if (task.IsFaulted)
            {
                throw task.Exception;
            }

            if (task.IsCanceled)
            {
                throw new Exception("Timeout obtaining access token.");
            }
            return accessToken;
        }
    }
}