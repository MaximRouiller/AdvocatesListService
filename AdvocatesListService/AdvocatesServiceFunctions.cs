using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Octokit;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using AdvocatesListService.Model;

namespace AdvocatesListService
{
    public static class AdvocatesServiceFunctions
    {
        private static string GitHubToken = Environment.GetEnvironmentVariable("GitHubToken");

        [FunctionName(nameof(Advocates))]
        public static async Task<List<AdvocateMapping>> Advocates(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            var github = new GitHubClient(new ProductHeaderValue("cloud-advocate-parser"));
            
            var tokenAuth = new Credentials(GitHubToken);
            github.Credentials = tokenAuth;

            var contents = await github
                .Repository
                .Content
                .GetAllContents("MicrosoftDocs", "cloud-developer-advocates", "advocates");


            IEnumerable<RepositoryContent> advocatesYamls = contents
                .Where(x => Path.GetExtension(x.Path) == ".yml")
                .Where(x=> !x.Path.EndsWith("index.html.yml"))
                .Where(x=> !x.Path.EndsWith("index.yml"))
                .Where(x=> !x.Path.EndsWith("toc.yml"))
                .Where(x=> !x.Path.EndsWith("tweets.yml"))
                .Where(x=> !x.Path.EndsWith("map.yml"));

            Regex msAuthorRegex = new Regex("ms.author: (?<msauthor>.*)");
            Regex teamRegEx = new Regex("team: (?<team>.*)");
            Regex githubUsernameRegex = new Regex("url: https?://github.com/(?<username>.+)/?");

            var advocatesResult = new List<AdvocateMapping>();

            foreach (var gitAdvocate in advocatesYamls)
            {
                var fileReference = await github.Git.Blob.Get("MicrosoftDocs", "cloud-developer-advocates", gitAdvocate.Sha);
                var content = Base64Decode(fileReference.Content);
                try
                {
                    advocatesResult.Add(new AdvocateMapping
                    {
                        GitHubUsername = githubUsernameRegex.Match(content).Groups["username"]?.Value.TrimEnd(),
                        MicrosoftAlias = msAuthorRegex.Match(content).Groups["msauthor"]?.Value.TrimEnd(),
                        Team = teamRegEx.Match(content).Groups["team"]?.Value.TrimEnd(),
                    });
                }
                catch (Exception ex)
                {
                    log.LogError(ex, $"Error while parsing {gitAdvocate.Path}");
                }
            }


            return advocatesResult;
        }

        private static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
    }
}
