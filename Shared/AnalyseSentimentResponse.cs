using System.Collections.Generic;

namespace durable_functions_sample.Shared
{
    public class AnalyseSentimentResponse
    {
        public AnalyseSentimentResponse()
        {
            Documents = new List<Document>();
        }

        public class Document
        {
            public string Id { get; set; }
            public long Score { get; set; }

            public string InferredSentiment
            {
                get
                {
                    if (Score >= 0 && Score < 0.4)
                    {
                        return "Negative";
                    }

                    if (Score >= 0.4 && Score < 0.6)
                    {
                        return "Neutral";
                    }

                    if (Score > 0.6)
                    {
                        return "Positive";
                    }

                    return "Neutral";
                }
            }
        }

        public IList<Document> Documents { get; set; }
    }
}
