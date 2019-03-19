using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace durable_functions_sample
{

    public static class CreateGithubRepo
    {
        [FunctionName("CreateGithubRepo")]
        public static async Task Run([ActivityTrigger] string name)
        {
            await Task.CompletedTask;
        }
    }
}