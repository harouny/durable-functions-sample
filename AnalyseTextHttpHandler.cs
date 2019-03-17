using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
    public class Request
    {
        public string RepoName { get; set; }
        public string[] Documents { get; set; }
    }

    public class DataItem
    {
        public Issue Issue { get; set; }
        public Document Document { get; set; }
    }

    public static class AnalyseTextHttpHandler
    {
        [FunctionName("AnalyseTextHttpHandler")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            var requestBody = await req.ReadAsStringAsync();
            var request = JsonConvert.DeserializeObject<Request>(requestBody);

            // 1 - create a github repo
            await SharedServices.GitHubClient.Repository.Create(new NewRepository(request.RepoName));

            var items = new Dictionary<string, DataItem>();

            // 2- create github issue for every document
            foreach (var document in request.Documents)
            {
                var createIssue = new NewIssue(document.Length > 150 ? document.Substring(0, 150) : document)
                {
                    Body = document
                };
                var issue = await SharedServices.GitHubClient.Issue.Create("harouny", request.RepoName, createIssue);
                items.Add(issue.Number.ToString(), new DataItem
                {
                    Issue = issue,
                    Document = new Document
                    {
                        Id = issue.Number.ToString(),
                        Text = document
                    },
                });
            }
            

            // 3- detect languages
            var languagesResponse = await SharedServices.CognitiveServicesHttpClient
                .PostAsJsonAsync("/text/analytics/v2.0/languages", new AnalyseTextRequest
                {
                    Documents = items.Select(item => item.Value.Document).ToList()
                });
            languagesResponse.EnsureSuccessStatusCode();
            var languagesBody = await languagesResponse.Content.ReadAsStringAsync();
            var languages = JsonConvert.DeserializeObject<DetectLanguageResponse>(languagesBody);
            
            // apply detected language to items
            foreach (var doc in languages.Documents)
            {
                // 4- add detected language issue label
                var issueUpdate = items[doc.Id].Issue.ToUpdate();
                issueUpdate.AddLabel(doc.InferredLanguageName);
                var issue = await SharedServices.GitHubClient.Issue.Update("harouny", request.RepoName, items[doc.Id].Issue.Number,
                    issueUpdate);
                // set inferred language to each document
                items[doc.Id].Document.Language = doc.InferredLanguage;
                items[doc.Id].Issue = issue;
            }
            

            // 5- analyse sentiment
            var sentimentResponse = await SharedServices.CognitiveServicesHttpClient
                .PostAsJsonAsync("/text/analytics/v2.0/sentiment", new AnalyseTextRequest
                {
                    Documents = items.Select(item => item.Value.Document).ToList(),
                });
            sentimentResponse.EnsureSuccessStatusCode();
            var sentimentBody = await sentimentResponse.Content.ReadAsStringAsync();
            var sentiments = JsonConvert.DeserializeObject<AnalyseSentimentResponse>(sentimentBody);
           
            foreach (var doc in sentiments.Documents)
            {
                // 6- add detected sentiments issue label
                var issueUpdate = items[doc.Id].Issue.ToUpdate();
                issueUpdate.AddLabel(doc.InferredSentiment);
                var issue = await SharedServices.GitHubClient.Issue.Update("harouny", request.RepoName, int.Parse(doc.Id),
                    issueUpdate);
                items[doc.Id].Issue = issue;
            }

            // return all github issue urls
            return new OkObjectResult(items.Values.Select(v => v.Issue.HtmlUrl));
        }
    }
}
