using System.Collections.Generic;

namespace durable_functions_sample.Shared
{
    public class AnalyseTextRequest
    {
        public IList<Document> Documents { get; set; }
    }
}
