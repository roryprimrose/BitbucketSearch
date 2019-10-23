namespace BitbucketSearch
{
    using CommandLine;
    using Microsoft.Extensions.Logging;
    using System.Linq;

    internal class Program
    {
        private static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(x =>
                {
                    var level = x.Verbose ? LogLevel.Debug : LogLevel.Information;

                    using (var factory = new LoggerFactory())
                    {
                        factory.AddProvider(new CustomLoggerProvider(level, "{formatter(state, exception)}"));

                        var log = factory.CreateLogger<Program>();

                        var credential = CredentialResolver.GetCredentials(x);

                        if (credential == null)
                        {
                            log.LogError("No credentials provided");

                            return;
                        }

                        var engine = new SearchEngine(x, credential, log);

                        var results = engine.Run().GetAwaiter().GetResult();

                        OutputReport(results, log);

                        log.LogInformation("Completed");
                    }
                });
        }

        private static void OutputReport(Results results, ILogger log)
        {
            log.LogInformation($"Processed {results.ProjectCount} projects");
            log.LogInformation($"Processed {results.RepoCount} repositories");
            log.LogInformation($"Processed {results.BranchCount} branches");
            log.LogInformation($"Found {results.Matches.Count} matches");

            foreach (var match in results.Matches.OrderBy(x => x))
            {
                log.LogInformation(match);
            }
        }
    }
}