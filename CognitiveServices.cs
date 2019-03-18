using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using durable_functions_sample.Shared;
using Newtonsoft.Json;
using Octokit;

namespace durable_functions_sample
{
    public class CognitiveServices
    {
        public static async Task<IList<DetectLanguageResponse.Document>> DetectLanguagesAsync(List<Document> documents)
        {
            var languagesResponse = await SharedServices.CognitiveServicesHttpClient
                .PostAsJsonAsync("/text/analytics/v2.0/languages", new AnalyseTextRequest
                {
                    Documents = documents,
                });
            languagesResponse.EnsureSuccessStatusCode();
            var languagesBody = await languagesResponse.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<DetectLanguageResponse>(languagesBody).Documents;
        }

        public static async Task<IList<AnalyseSentimentResponse.Document>> AnalyseSentimentAsync(IList<Document> documents)
        {
            var sentimentResponse = await SharedServices.CognitiveServicesHttpClient
                .PostAsJsonAsync("/text/analytics/v2.0/sentiment", new AnalyseTextRequest
                {
                    Documents = documents,
                });
            sentimentResponse.EnsureSuccessStatusCode();
            var sentimentBody = await sentimentResponse.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<AnalyseSentimentResponse>(sentimentBody).Documents;
        }
    }
}
