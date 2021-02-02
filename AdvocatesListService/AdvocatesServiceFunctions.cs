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
using Microsoft.WindowsAzure.Storage;
using System.Text.Json;

namespace AdvocatesListService
{
    public static class AdvocatesServiceFunctions
    {
        private static string GitHubToken = Environment.GetEnvironmentVariable("GitHubToken");

        [FunctionName(nameof(UpdateAdvocatesTimerAsync))]
        public static async Task UpdateAdvocatesTimerAsync([TimerTrigger("0 0 13 * * *")] TimerInfo myTimer, ILogger log)
        {
            List<AdvocateMapping> advocatesResult = await GenerateAdvocatesList(log);
            await SaveToBlobStorageAsync(advocatesResult);
        }

        private static async Task<List<AdvocateMapping>> GenerateAdvocatesList(ILogger log)
        {
            var github = new GitHubClient(new ProductHeaderValue("cloud-advocate-parser"));

            var tokenAuth = new Credentials(GitHubToken);
            github.Credentials = tokenAuth;

            var contents = await github
                .Repository
                .Content
                .GetAllContents("MicrosoftDocs", "cloud-developer-advocates", "advocates");


            IEnumerable<RepositoryContent> advocatesYamls = contents
                .Where(x => Path.GetExtension(x.Path) == ".yml")
                .Where(x => !x.Path.EndsWith("index.html.yml"))
                .Where(x => !x.Path.EndsWith("index.yml"))
                .Where(x => !x.Path.EndsWith("toc.yml"))
                .Where(x => !x.Path.EndsWith("tweets.yml"))
                .Where(x => !x.Path.EndsWith("map.yml"));

            Regex msAuthorRegex = new Regex("ms.author: (?<msauthor>.*)");
            Regex teamRegEx = new Regex("team: (?<team>.*)");
            Regex githubUsernameRegex = new Regex("url: https?://github.com/(?<username>.+)/?");


            List<AdvocateMapping> advocatesResult = new List<AdvocateMapping>();
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

        [FunctionName(nameof(Advocates))]
        public static async Task<List<AdvocateMapping>> Advocates(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            List<AdvocateMapping> advocatesResult = await GetResultFromBlobStorageAsync();

            if(advocatesResult == null)
            {
                advocatesResult = await GenerateAdvocatesList(log);
                await SaveToBlobStorageAsync(advocatesResult);
            }
            return advocatesResult;
        }

        private static async Task SaveToBlobStorageAsync(List<AdvocateMapping> advocatesResult)
        {
            CloudStorageAccount account;
            if (CloudStorageAccount.TryParse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"), out account))
            {
                var client = account.CreateCloudBlobClient();
                var container = client.GetContainerReference("cache");
                await container.CreateIfNotExistsAsync();

                var advocatesFile = container.GetBlockBlobReference("advocates.json");
                await advocatesFile.UploadTextAsync(JsonSerializer.Serialize(advocatesResult));
            }
        }

        private static async Task<List<AdvocateMapping>> GetResultFromBlobStorageAsync()
        {
            CloudStorageAccount account;
            if (!CloudStorageAccount.TryParse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"), out account))
            {
                return null;
            }

            var client = account.CreateCloudBlobClient();
            var container = client.GetContainerReference("cache");
            await container.CreateIfNotExistsAsync();

            var advocatesFile = container.GetBlockBlobReference("advocates.json");
            if (!await advocatesFile.ExistsAsync()) return null;

            string jsonContent = await advocatesFile.DownloadTextAsync();
            return JsonSerializer.Deserialize<List<AdvocateMapping>>(jsonContent);
        }

        private static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
    }
}
