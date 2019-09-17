namespace BitbucketSearch
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json.Linq;
    using Meziantou.Framework.Win32;

    public class SearchEngine
    {
        private readonly Credential _credential;
        private readonly ILogger _log;
        private readonly Options _options;
        private const int MaxEntries = 10000;

        public SearchEngine(Options options, Credential credential, ILogger log)
        {
            _options = options;
            _credential = credential;
            _log = log;
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
            var branchId = ((string)branch.id).Replace("/", "%2F");

            var targetUri = new Uri(_options.Server,
                $"/projects/{project.key}/repos/{repo.slug}/{purpose}/{path}?at=/{branchId}");

            return targetUri;
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

        private async Task ProcessBranch(dynamic project, dynamic repo, dynamic branch, Results results)
        {
            _log.LogInformation($"Processing branch {branch.displayId} repo {project.name}/{repo.name}");

            String branchId = ((string)branch.displayId).Replace("/", "%2F");

            // Get the file paths in the branch
            var response = await ReadJsonData($"{project.key}/repos/{repo.slug}/files?at={branchId}").ConfigureAwait(false);

            List<string> files = ((JArray)response.values).Select(x => x.Value<string>()).ToList();
            List<string> filesToCheck = files;

            // Check how we are searching the branch
            if (_options.PathMatcher != null)
            {
                // Resolve the specific file paths that match the expression
                filesToCheck = files.Where(x => _options.PathMatcher.IsMatch(x)).ToList();
            }

            if (filesToCheck.Count == 0)
            {
                return;
            }

            _log.LogInformation($"Checking {filesToCheck.Count} of {files.Count} files in branch {branch.displayId} repo {project.name}/{repo.name}");

            var tasks = filesToCheck.Select<dynamic, Task>(x => ProcessFile(project, repo, branch, x, results));

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private async Task ProcessFile(dynamic project, dynamic repo, dynamic branch, string path, Results results) 
        {
            if (await IsFileMatch(project, repo, branch, path).ConfigureAwait(false))
            {
                // Build a uri for the path
                Uri targetUri = BuildFileUri(project, repo, branch, path, "browse");

                results.Matches.Add(targetUri.AbsoluteUri);
            }
        }

        private async Task<Boolean> IsFileMatch(dynamic project, dynamic repo, dynamic branch, String path) {
            // We have been asked to check a file that has been found
            if (_options.ContentMatcher == null) {
                // The file exists but we don't need to check the contents
                _log.LogInformation($"Found path match on branch {branch.displayId} repo {project.name}/{repo.name} for {path}");

                return true;
            }

            // Download the raw contents of the file and check the contents
            var contents = await ReadRawData(project, repo, branch, path).ConfigureAwait(false);

            if (_options.ContentMatcher.IsMatch(contents))
            {
                _log.LogInformation($"Found contents match on branch {branch.displayId} repo {project.name}/{repo.name} in {path}");

                return true;
            }

            return false;
        }

        private async Task<dynamic> ReadJsonData(string uri = "")
        {
            var baseUri = new Uri(_options.Server, "/rest/api/1.0/projects/");

            // Include the limit query
            if (uri.IndexOf("?") == -1) 
            {
                uri += "?limit=" + MaxEntries;
            }
            else
            {
                uri += "&limit=" + MaxEntries;
            }

            var targetUri = new Uri(baseUri, uri);

            _log.LogDebug("Reading from " + targetUri);

            using (var client = new HttpClient())
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, targetUri))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                        Convert.ToBase64String(
                            Encoding.ASCII.GetBytes($"{_credential.UserName}:{_credential.Password}")));

                    using (var result = await client.SendAsync(request).ConfigureAwait(false))
                    {
                        if (result.StatusCode == HttpStatusCode.NotFound)
                        {
                            return null;
                        }
                        
                        var content = await result.Content.ReadAsStringAsync().ConfigureAwait(false);

                        if (result.StatusCode != HttpStatusCode.OK)
                        {
                            _log.LogError("Failed to read from " + targetUri.AbsoluteUri + Environment.NewLine + Environment.NewLine + content);

                            throw new InvalidOperationException("Failed to read from Bitbucket");
                        }

                        dynamic data = JObject.Parse(content);

                        if ((int) data.size == _options.PageSize)
                        {
                            _log.LogWarning("The maximum number of items has been returned from " + targetUri + " and paging is not yet supported");
                        }

                        return data;
                    }
                }
            }
        }

        private async Task<dynamic> ReadRawData(dynamic project, dynamic repo, dynamic branch, string path)
        {
            Uri targetUri = BuildFileUri(project, repo, branch, path, "raw");

            _log.LogDebug("Reading from " + targetUri);

            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, targetUri);

                request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(
                        Encoding.ASCII.GetBytes($"{_credential.UserName}:{_credential.Password}")));

                var result = await client.SendAsync(request).ConfigureAwait(false);

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
}