using System;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Octokit;

namespace durable_functions_sample.Shared
{
    public static class SharedServices
    {
        public static HttpClient HttpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(5),
        };

        public static HttpClient CognitiveServicesHttpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(5),
            DefaultRequestHeaders =
            {
                { "Ocp-Apim-Subscription-Key", Environment.GetEnvironmentVariable("CognitiveServicesApiKey") },
                { "Accept", "application/json" }
            },
            BaseAddress = new Uri($"https://{Environment.GetEnvironmentVariable("CognitiveServiceRegion")}.api.cognitive.microsoft.com")
        };

        public static GitHubClient GitHubClient = new GitHubClient(new ProductHeaderValue("durable-functions-demo"))
        {
            Credentials = new Credentials(Environment.GetEnvironmentVariable("GithubToken")),
        };

        public static void SetupJsonSettings()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
        }
    }
}
