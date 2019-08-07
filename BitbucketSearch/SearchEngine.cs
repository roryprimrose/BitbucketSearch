namespace BitbucketSearch
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json.Linq;

    public class SearchEngine
    {
        private readonly HttpClient _client;
        private readonly Regex _contentExpression;
        private readonly ILogger _log;
        private readonly Options _options;
        private readonly Regex _pathExpression;

        public SearchEngine(Options options, HttpClient client, ILogger log)
        {
            _options = options;
            _client = client;
            _log = log;

            if (string.IsNullOrWhiteSpace(_options.ContentMatcher) == false)
            {
                _contentExpression = new Regex(_options.ContentMatcher);
            }

            if (string.IsNullOrWhiteSpace(_options.FileMatcher) == false)
            {
                _pathExpression = new Regex(_options.FileMatcher);
            }
        }

        public async Task<Results> Run()
        {
            var results = new Results();

            // Load the projects
            var response = await ReadJsonData().ConfigureAwait(false);

            _log.LogInformation($"Processing {response.size} projects");

            results.ProjectCount = response.size;

            IEnumerable<dynamic> projects = response.values;

            var tasks = projects.Select<dynamic, Task>(x => ProcessProject(x, results));

            await Task.WhenAll(tasks).ConfigureAwait(false);

            return results;
        }

        private Uri BuildFileUri(dynamic project, dynamic repo, dynamic branch, string path, string purpose)
        {
            var targetUri = new Uri(_options.Server,
                $"/rest/api/1.0/projects/{project.key}/repos/{repo.slug}/branches/{branch.id}/{purpose}/" + path);

            return targetUri;
        }

        private async Task<bool> CheckFileExists(dynamic project, dynamic repo, dynamic branch, string path)
        {
            Uri targetUri = BuildFileUri(project, repo, branch, path, "raw");

            _log.LogDebug("Reading from " + targetUri);

            var request = new HttpRequestMessage(HttpMethod.Head, targetUri);

            var result = await _client.SendAsync(request).ConfigureAwait(false);

            if (result.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }

            return true;
        }

        private async Task ProcessBranch(dynamic project, dynamic repo, dynamic branch, Results results)
        {
            _log.LogInformation($"Processing branch {branch.name} repo {project.name}/{repo.name}");

            // Check how we are searching the branch
            if (string.IsNullOrWhiteSpace(_options.FilePath) == false)
            {
                await ProcessFilePath(project, repo, branch, _options.FilePath, results).ConfigureAwait(false);
            }
            else if (_pathExpression != null)
            {
                // Find all the paths in the branch that match the expression
            }
        }

        private async Task ProcessFilePath(
            dynamic project,
            dynamic repo,
            dynamic branch,
            string filePath,
            Results results)
        {
            // Determine the browse uri for the specific path on the branch
            var browseUri = new Uri(_options.Server,
                $"/rest/api/1.0/projects/{project.key}/repos/{repo.slug}/branches/{branch.id}/browse/" + filePath);

            // Resolve the specific file path
            if (_contentExpression == null)
            {
                // We don't care about the file contents, only if the file exists
                // Issue a HEAD request against the raw content to resolve whether the file exists
                var exists = await CheckFileExists(project, repo, branch, filePath).ConfigureAwait(false);

                if (exists)
                {
                    results.Matches.Add(browseUri.AbsoluteUri);
                }
            }
            else
            {
                // We are checking that the file exists but also that the contents matches the expression
                var content = await ReadRawData(project, repo, branch, filePath).ConfigureAwait(false);

                if (content == null)
                {
                    // The resource doesn't exist
                    return;
                }

                if (_contentExpression.IsMatch(content))
                {
                    results.Matches.Add(browseUri.AbsoluteUri);
                }
            }
        }

        private async Task ProcessProject(dynamic project, Results results)
        {
            _log.LogInformation($"Processing project {project.name}");

            var response = await ReadJsonData($"{project.key}/repos/").ConfigureAwait(false);

            _log.LogInformation($"Processing {response.size} repos in the {project.name} project");

            results.RepoCount += (int) response.size;

            IEnumerable<dynamic> repos = response.values;

            var tasks = repos.Select<dynamic, Task>(x => ProcessRepo(project, x, results));

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private async Task ProcessRepo(dynamic project, dynamic repo, Results results)
        {
            _log.LogInformation($"Processing repo {project.name}/{repo.name}");

            var response = await ReadJsonData($"{project.key}/repos/{repo.slug}/branches").ConfigureAwait(false);

            _log.LogInformation($"Processing {response.size} branches in the {project.name}/{repo.name} repo");

            results.BranchCount += (int) response.size;

            IEnumerable<dynamic> branches = response.values;

            var tasks = branches.Select<dynamic, Task>(x => ProcessBranch(project, repo, x, results));

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private async Task<dynamic> ReadJsonData(string uri = "")
        {
            var baseUri = new Uri(_options.Server, "/rest/api/1.0/projects/");
            var targetUri = new Uri(baseUri, uri + "?limit=" + _options.PageSize);

            _log.LogDebug("Reading from " + targetUri);

            var request = new HttpRequestMessage(HttpMethod.Get, targetUri);

            var result = await _client.SendAsync(request).ConfigureAwait(false);

            if (result.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            var content = await result.Content.ReadAsStringAsync().ConfigureAwait(false);

            dynamic data = JObject.Parse(content);

            if ((int) data.size == _options.PageSize)
            {
                _log.LogWarning("The maximum number of items has been returned from " + targetUri
                                                                                      + " and paging is not yet supported");
            }

            return data;
        }

        private async Task<dynamic> ReadRawData(dynamic project, dynamic repo, dynamic branch, string path)
        {
            Uri targetUri = BuildFileUri(project, repo, branch, path, "raw");

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