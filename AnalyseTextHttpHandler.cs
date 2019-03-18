using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using durable_functions_sample.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Octokit;

namespace durable_functions_sample
{
    public static class AnalyseTextHttpHandler
    {
        [FunctionName("AnalyseTextHttpHandler")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            var requestBody = await req.ReadAsStringAsync();
            var request = JsonConvert.DeserializeObject<Request>(requestBody);
            var items = new Dictionary<string, DataItem>();

            // 1 - create a github repo
            await Github.CreateRepoAsync(request.RepoName);

            
            // 2- create github issue for every document
            foreach (var text in request.Texts)
            {
                var issue = await Github.CreateIssueAsync(
                    text: text, 
                    repoName: request.RepoName);

                items.Add(issue.Number.ToString(), new DataItem
                {
                    Issue = issue, // github issue
                    Document = new Document // cognitive services Document
                    {
                        Id = issue.Number.ToString(),
                        Text = text,
                        Language = null,
                    },
                });
            }
            
            // 3- detect languages
            var languages = await CognitiveServices.DetectLanguagesAsync(
                documents: items.Select(i => i.Value.Document).ToList());
            
            foreach (var languageInfo in languages)
            {
                // set document language
                items[languageInfo.Id].Document.Language = languageInfo.InferredLanguage;

                // 4- add detected language issue label
                var issue = await Github.AddLabelAsync(
                        issue: items[languageInfo.Id].Issue,
                        repoName: request.RepoName, 
                        label: languageInfo.InferredLanguageName);
                items[languageInfo.Id].Issue = issue;
            }
            

            // 5- analyse sentiment
            var sentiments = await CognitiveServices.AnalyseSentimentAsync(
                documents: items.Select(i => i.Value.Document).ToList());
           
            foreach (var sentimentInfo in sentiments)
            {
                // 6- add detected sentiments issue label
                var issue = await Github.AddLabelAsync(
                    issue: items[sentimentInfo.Id].Issue,
                    repoName: request.RepoName,
                    label: sentimentInfo.InferredSentiment);
                items[sentimentInfo.Id].Issue = issue;
            }


            // return all github issue urls
            return new OkObjectResult(items.Values.Select(v => v.Issue.HtmlUrl));
        }
    }

    public class Request
    {
        public string RepoName { get; set; }
        public string[] Texts { get; set; }
    }

    public class DataItem
    {
        public Issue Issue { get; set; }
        public Document Document { get; set; }
    }
}
