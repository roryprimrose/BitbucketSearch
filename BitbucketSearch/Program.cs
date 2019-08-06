namespace BitbucketSearch
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading.Tasks;
    using CommandLine;
    using Microsoft.Extensions.Logging;

    internal class Program
    {
        private static async Task Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(x =>
                {
                    using (var client = new HttpClient())
                    {
                        var level = x.Verbose ? LogLevel.Debug : LogLevel.Information;

                        using (var factory = new LoggerFactory().AddConsole(level))
                        {
                            var log = factory.CreateLogger<Program>();

                            var credential = CredentialResolver.GetCredentials(x);

                            if (credential == null)
                            {
                                log.LogError("No credentials provided");

                                return;
                            }

                            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                                Convert.ToBase64String(
                                    Encoding.ASCII.GetBytes($"{credential.UserName}:{credential.Password}")));

                            var engine = new SearchEngine(x, client, log);

                            var results = engine.Run().GetAwaiter().GetResult();

                            OutputReport(results, log);

                            log.LogInformation("Completed");
                        }
                    }
                });
        }

        private static void OutputReport(Results results, ILogger log)
        {
            log.LogInformation($"Processed {results.ProjectCount} projects");
            log.LogInformation($"Processed {results.RepoCount} repositories");
            log.LogInformation($"Processed {results.BranchCount} branches");
        }
    }
}