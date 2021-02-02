using System;

namespace AdvocatesListService.Model
{
    public class AdvocateMapping
    {
        public string GitHubUsername { get; set; }
        public string MicrosoftAlias { get; set; }
        public string Team { get; set; }

        public override bool Equals(object obj)
        {
            return obj is AdvocateMapping other &&
                   GitHubUsername == other.GitHubUsername &&
                   MicrosoftAlias == other.MicrosoftAlias &&
                   Team == other.Team;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(GitHubUsername, MicrosoftAlias, Team);
        }
    }
}
