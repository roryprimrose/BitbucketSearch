namespace BitbucketSearch
{
    using CommandLine;
    using System;
    using System.Text.RegularExpressions;

    public class Options
    {
        private Regex _contentsMatcher = null;
        private Regex _pathMatcher = null;

        [Option('c', "contentsExpression", Required = false, HelpText = "The regular expression to use against file contents.")]
        public string ContentsExpression { get; set; }

        [Option('p', "pathExpression", Required = false, HelpText = "The regular expression to use against file paths.")]
        public string PathExpression { get; set; }

        public Regex ContentsMatcher {
            get {
                if (_contentsMatcher != null) {
                    return _contentsMatcher;
                }

                if (string.IsNullOrWhiteSpace(ContentsExpression)) {
                    return null;
                }

                _contentsMatcher = new Regex(ContentsExpression);

                return _contentsMatcher;
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

        [Option('u', "serverUri", Required = true, HelpText = "URI for the Bitbucket API.")]
        public string ServerUri { get; set; }

        public Uri Server => new Uri(ServerUri);

        [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
        public bool Verbose { get; set; }
    }
}