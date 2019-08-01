namespace BitbucketSearch
{
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json.Linq;

    public static class SearchEngine
    {
        public static async Task Run(Options options, HttpClient client, ILogger log)
        {
        }

        private static async Task<dynamic> ReadJsonData(HttpClient client, string uri)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, uri);

            var result = await client.SendAsync(request).ConfigureAwait(false);

            if (result.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            var content = await result.Content.ReadAsStringAsync().ConfigureAwait(false);

            return JObject.Parse(content);
        }
    }
}