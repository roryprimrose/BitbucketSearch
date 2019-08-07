﻿namespace BitbucketSearch
{
    using System;
    using System.Text.RegularExpressions;
    using CommandLine;

    public class Options
    {
        [Option('c',
            "contentMatcher",
            Required = false,
            HelpText = "The regular expression to check against file contents.")]
        public string ContentMatcher { get; set; }

        [Option('m',
            "fileMatcher",
            Required = false,
            HelpText = "The regular expression to check against repo file paths.")]
        public string FileMatcher { get; set; }

        [Option('f', "filePath", Required = false, HelpText = "The file path to check.")]
        public string FilePath { get; set; }

        [Option('s',
            "pageSize",
            Required = false,
            HelpText = "Set the API pagination size. Defaults to 10000.",
            Default = 10000)]
        public int PageSize { get; set; }

        public Uri Server => new Uri(ServerUri);

        [Option('u', "serverUri", Required = true, HelpText = "URI for the Bitbucket API.")]
        public string ServerUri { get; set; }

        [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
        public bool Verbose { get; set; }
    }
}