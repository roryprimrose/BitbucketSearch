namespace BitbucketSearch
{
    using System;
    using System.Text.RegularExpressions;
    using CommandLine;

    public class Options
    {
        private Regex _contentMatcher = null;
        private Regex _pathMatcher = null;

        [Option('c', "contentsExpression", Required = false, HelpText = "The regular expression to use against file contents.")]
        public string ContentsExpression { get; set; }

        [Option('p', "pathExpression", Required = false, HelpText = "The regular expression to use against file paths.")]
        public string PathExpression { get; set; }

        public Regex ContentMatcher {
            get {
                if (_contentMatcher != null) {
                    return _contentMatcher;
                }

                if (string.IsNullOrWhiteSpace(ContentsExpression)) {
                    return null;
                }

                _contentMatcher = new Regex(ContentsExpression);

                return _contentMatcher;
            }
        }

        public Regex PathMatcher {
            get {
                if (_pathMatcher != null) {
                    return _pathMatcher;
                }

                if (string.IsNullOrWhiteSpace(PathExpression)) {
                    return null;
                }

                _pathMatcher = new Regex(PathExpression);

                return _pathMatcher;
            }
        }

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