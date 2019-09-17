using System.Collections.Generic;

namespace BitbucketSearch
{
    public class Results
    {
        public int BranchCount {get; set;}
        public List<string> Matches => new List<string>();
        public int ProjectCount {get; set;}
        public int RepoCount {get; set;}
    }
}