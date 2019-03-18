using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using durable_functions_sample.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Octokit;

namespace durable_functions_sample
{
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

    public static class AnalyseTextHttpHandler
    {
        [FunctionName("AnalyseTextHttpHandler")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            var requestBody = await req.ReadAsStringAsync();
            var request = JsonConvert.DeserializeObject<Request>(requestBody);
            var items = new Dictionary<string, DataItem>();

            #region create a github repo
                await Github.CreateRepoAsync(request.RepoName);
            #endregion

            #region create github issue for every text item
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
            #endregion

            #region detect languages with cognitive services
            var languages = await CognitiveServices.DetectLanguagesAsync(
                    documents: items.Select(i => i.Value.Document).ToList());
            #endregion

            #region add github labguage labels
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
            #endregion

            #region analyse sentiment with cognitive services
            var sentiments = await CognitiveServices.AnalyseSentimentAsync(
                    documents: items.Select(i => i.Value.Document).ToList());
            #endregion

            #region add github sentiment labels
            foreach (var sentimentInfo in sentiments)
            {
                var issue = await Github.AddLabelAsync(
                    issue: items[sentimentInfo.Id].Issue,
                    repoName: request.RepoName,
                    label: sentimentInfo.InferredSentiment);
                items[sentimentInfo.Id].Issue = issue;
            } 
            #endregion

            // return all github issue urls
            return new OkObjectResult(items.Values.Select(v => v.Issue.HtmlUrl));
        }
    }
}
