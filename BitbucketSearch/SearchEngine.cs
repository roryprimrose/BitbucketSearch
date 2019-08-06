namespace BitbucketSearch
{
    using System;
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
            dynamic response = await ReadJsonData().ConfigureAwait(false);

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

            var response = await ReadJsonData($"{project.key}/repos/");

            _log.LogInformation($"Processing {response.size} repos in the {project.name} project");

            results.RepoCount += (int)response.size;

            IEnumerable<dynamic> repos = response.values;

            var tasks = repos.Select<dynamic, Task>(x => ProcessRepo(project, x, results));

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private async Task ProcessRepo(dynamic project, dynamic repo, Results results)
        {
            _log.LogInformation($"Processing repo {project.name}/{repo.name}");

            var response = await ReadJsonData($"{project.key}/repos/{repo.slug}/branches");

            _log.LogInformation($"Processing {response.size} branches in the {project.name}/{repo.name} repo");

            results.BranchCount += (int)response.size;

            IEnumerable<dynamic> branches = response.values;

            var tasks = branches.Select<dynamic, Task>(x => ProcessBranch(project, repo, x, results));

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private async Task ProcessBranch(dynamic project, dynamic repo, dynamic branch, Results results)
        {
            _log.LogInformation($"Processing branch {branch.name} repo {project.name}/{repo.name}");

            // Check how we are searching the branch
            if (string.IsNullOrWhiteSpace(_options.FilePath) == false)
            {
                // Resolve the specific file path

            }

        }

        private async Task<dynamic> ReadJsonData(string uri = "")
        {
            const int MaxEntries = 10000;
            var baseUri = new Uri(_options.Server, "/rest/api/1.0/projects/");
            var targetUri = new Uri(baseUri, uri + "?limit=" + MaxEntries);

            _log.LogDebug("Reading from " + targetUri);

            var request = new HttpRequestMessage(HttpMethod.Get, targetUri);

            var result = await _client.SendAsync(request).ConfigureAwait(false);

            if (result.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            var content = await result.Content.ReadAsStringAsync().ConfigureAwait(false);

            dynamic data = JObject.Parse(content);

            if ((int)data.size == MaxEntries) 
            {
                _log.LogWarning("The maximum number of items has been returned from " + targetUri + " and paging is not yet supported");
            }

            return data;
        }

        private async Task<dynamic> ReadRawData(dynamic project, dynamic repo, dynamic branch, string path)
        {
            var targetUri = new Uri(_options.Server, $"/rest/api/1.0/projects/{project.key}/repos/{repo.slug}/branches/{branch.id}");

            _log.LogDebug("Reading from " + targetUri);

            var request = new HttpRequestMessage(HttpMethod.Get, targetUri);

            var result = await _client.SendAsync(request).ConfigureAwait(false);

            if (result.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            var content = await result.Content.ReadAsStringAsync().ConfigureAwait(false);

            dynamic data = JObject.Parse(content);

            return data;
        }
    }
}