namespace BitbucketSearch
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json.Linq;

    public class SearchEngine
    {
        private Options _options;
        private HttpClient _client;
        private ILogger _log;

        public SearchEngine(Options options, HttpClient client, ILogger log)
        {
            _options = options;
            _client = client;
            _log = log;        
        }

        public async Task<Results> Run()
        {
            var results = new Results();

            // Load the projects
            dynamic response = await ReadJsonData("").ConfigureAwait(false);

            _log.LogInformation($"Processing {response.size} projects");

            results.ProjectCount = response.size;

            IEnumerable<dynamic> projects = response.values;

            var tasks = projects.Select<dynamic, Task>(x => ProcessProject(x, results));

            await Task.WhenAll(tasks).ConfigureAwait(false);

            return results;
        }

        private async Task ProcessProject(dynamic project, Results results)
        {
            _log.LogInformation($"Processing project {project.name}");

            var response = await ReadJsonData($"/{project.key}/repos/");

            _log.LogInformation($"Processing {response.size} repos in the {project.name} project");

            results.RepoCount += response.size;

            IEnumerable<dynamic> repos = response.values;
        }

        private async Task<dynamic> ReadJsonData(string uri)
        {
            // TODO: Need to handle leading and trailing / characters to avoid a borked uri
            // Probably use new Uri(base, rel) to combine values

            var baseUri = _options.ServerUri + "/rest/api/1.0/projects/";
            var targetUri = baseUri + uri + "?limit=1000";

            var request = new HttpRequestMessage(HttpMethod.Get, targetUri);

            var result = await _client.SendAsync(request).ConfigureAwait(false);

            if (result.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            var content = await result.Content.ReadAsStringAsync().ConfigureAwait(false);

            return JObject.Parse(content);
        }
    }
}