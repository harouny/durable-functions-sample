using durable_functions_sample.Shared;
using Microsoft.Azure.WebJobs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace durable_functions_sample
{
    public static class Activities
    {
        [FunctionName(nameof(CreateGithubRepoActivity))]
        public static async Task CreateGithubRepoActivity([ActivityTrigger] string repoName)
        {
            await Github.CreateRepoAsync(repoName);
        }

        [FunctionName(nameof(CreateIssueAsyncActivity))]
        public static async Task<(int number, string url)> CreateIssueAsyncActivity([ActivityTrigger] (string text, string repoName) input)
        {
            return await Github.CreateIssueAsync(
                text: input.text,
                repoName: input.repoName);
        }

        [FunctionName(nameof(DetectLanguagesActivity))]
        public static async Task<IList<DetectLanguageResponse.Document>> DetectLanguagesActivity([ActivityTrigger] List<Document> documents)
        {
            return await CognitiveServices.DetectLanguagesAsync(
                documents: documents);
        }

        [FunctionName(nameof(AnalyseSentimentActivity))]
        public static async Task<IList<AnalyseSentimentResponse.Document>> AnalyseSentimentActivity([ActivityTrigger] List<Document> documents)
        {
            return await CognitiveServices.AnalyseSentimentAsync(
                documents: documents);
        }


        [FunctionName(nameof(AddGithubLabelAsyncActivity))]
        public static async Task AddGithubLabelAsyncActivity([ActivityTrigger] (int issueNumber, string repoName, string label) input)
        {
            await Github.AddLabelAsync(
                issueNumber: input.issueNumber,
                repoName: input.repoName,
                label: input.label);
        }
    }
}
