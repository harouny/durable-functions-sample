using System;
using System.Threading.Tasks;
using durable_functions_sample.Shared;
using Octokit;

namespace durable_functions_sample
{
    public class Github
    {
        public static async Task CreateRepoAsync(string repoName)
        {
            await SharedServices.GitHubClient.Repository.Create(new NewRepository(repoName));
        }

        public static async Task<Issue> CreateIssueAsync(string text, string repoName)
        {
            var createIssue = new NewIssue(text.Length > 150 ? text.Substring(0, 150) : text)
            {
                Body = text
            };
            var issue = await SharedServices.GitHubClient.Issue.Create(
                Environment.GetEnvironmentVariable("GuthubOwner"), 
                repoName, 
                createIssue);
            return issue;
        }

        public static async Task<Issue> AddLabelAsync(Issue issue, string repoName, string label)
        {
            var issueUpdate = issue.ToUpdate();
            issueUpdate.AddLabel(label);
            return await SharedServices.GitHubClient.Issue.Update(
                Environment.GetEnvironmentVariable("GuthubOwner"), 
                    repoName, 
                    issue.Number,
                issueUpdate);
        }

    }
}
