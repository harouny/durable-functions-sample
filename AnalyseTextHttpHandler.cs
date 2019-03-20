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
        public Document Document { get; set; }
        public string GithubIssueHtmlUrl { get; set; }
    }


    public static class AnalyseTextOrchestration
    {
        [FunctionName(nameof(AnalyseTextOrchestration))]
        public static async Task<IEnumerable<string>> Run([OrchestrationTrigger] DurableOrchestrationContext context)
        {
            var request = context.GetInput<Request>();
            var items = new Dictionary<string, DataItem>();

            #region create a github repo
            await context.CallActivityAsync(
                functionName: nameof(Activities.CreateGithubRepoActivity), 
                input: request.RepoName);
            #endregion

            #region create github issue for every text item
            var tasks = request.Texts
                    .Select(text => 
                        context.CallActivityAsync<(int issueNumber, string issueHtmlUrl)>(
                            functionName: nameof(Activities.CreateIssueAsyncActivity), 
                            input: (text, request.RepoName)))
                    .ToList();

            await Task.WhenAll(tasks);

            for (var i = 0; i < request.Texts.Length; i++)
            {
                var (issueNumber, issueUrl) = tasks[i].Result;
                items.Add(issueNumber.ToString(), new DataItem
                {
                    Document = new Document
                    {
                        Id = issueNumber.ToString(),
                        Text = request.Texts[i],
                        Language = null,
                    },
                    GithubIssueHtmlUrl = issueUrl
                });
            }
            #endregion

            #region detect languages with cognitive services
            var languages = await context.CallActivityAsync<IList<DetectLanguageResponse.Document>>(
                    functionName: nameof(Activities.DetectLanguagesActivity),
                    input: items.Select(i => i.Value.Document));
            #endregion

            #region add github labguage labels
            foreach (var languageInfo in languages)
            {
                // set document language
                items[languageInfo.Id].Document.Language = languageInfo.InferredLanguage;

                await context.CallActivityAsync<Issue>(
                    functionName: nameof(Activities.AddGithubLabelAsyncActivity), 
                    input: (int.Parse(languageInfo.Id), 
                            request.RepoName, 
                            languageInfo.InferredLanguageName));
            }
            #endregion

            #region analyse sentiment with cognitive services
            var sentiments = await context.CallActivityAsync<IList<AnalyseSentimentResponse.Document>>(
                functionName: nameof(Activities.AnalyseSentimentActivity),
                input: items.Select(i => i.Value.Document));
            #endregion

            #region add github sentiment labels
            foreach (var sentimentInfo in sentiments)
            {
                await context.CallActivityAsync<Issue>(
                    functionName: nameof(Activities.AddGithubLabelAsyncActivity),
                    input: (int.Parse(sentimentInfo.Id),
                            request.RepoName,
                            sentimentInfo.InferredSentiment));
            }
            #endregion

            // return all github issue urls
            return items.Values.Select(v => v.GithubIssueHtmlUrl);
        }


        [FunctionName(nameof(AnalyseTextHttpHandler))]
        public static async Task<IActionResult> AnalyseTextHttpHandler(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req, [OrchestrationClient] DurableOrchestrationClient orchestrationClient)
        {
            var requestBody = await req.ReadAsStringAsync();
            var request = JsonConvert.DeserializeObject<Request>(requestBody);
            var instanceId = await orchestrationClient.StartNewAsync("AnalyseTextOrchestration", request);
            return new OkObjectResult(orchestrationClient.CreateHttpManagementPayload(instanceId));
        }

        
    }
}
