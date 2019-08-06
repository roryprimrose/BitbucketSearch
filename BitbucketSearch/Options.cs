namespace BitbucketSearch
{
    using System;
    using CommandLine;

    public class Options
    {
        [Option('m', "fileMatcher", Required = false, HelpText = "The file match regular expression to check.")]
        public string FileMatcher { get; set; }

        [Option('f', "filePath", Required = false, HelpText = "The file path to check.")]
        public string FilePath { get; set; }

        public Uri Server => new Uri(ServerUri);

        [Option('u', "serverUri", Required = true, HelpText = "URI for the Bitbucket API.")]
        public string ServerUri { get; set; }

        [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
        public bool Verbose { get; set; }
    }
}