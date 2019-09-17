using System.Collections.Generic;

namespace BitbucketSearch
{
    using System.Collections.Generic;

    public class Results
    {
        public int BranchCount { get; set; }

        public List<string> Matches { get; } = new List<string>();

        public int ProjectCount { get; set; }

        public int RepoCount { get; set; }
    }
}