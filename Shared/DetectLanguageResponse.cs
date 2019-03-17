using System;
using System.Collections.Generic;
using System.Linq;

namespace durable_functions_sample.Shared
{
    public class DetectLanguageResponse
    {
        public DetectLanguageResponse()
        {
            Documents = new List<Document>();
        }

        public class Document
        {
            public Document()
            {
                DetectedLanguages = new List<Language>();
            }
            public string Id { get; set; }
            public IList<Language> DetectedLanguages { get; set; }

            public string InferredLanguage => DetectedLanguages
                .FirstOrDefault(l => l.Score == DetectedLanguages.Max(l2 => l2.Score))?.Iso6391Name;
            public string InferredLanguageName => DetectedLanguages
                .FirstOrDefault(l => l.Score == DetectedLanguages.Max(l2 => l2.Score))?.Name;

            public string ContentModerationLanguageCode
            {
                get
                {
                    switch (InferredLanguage)
                    {
                        case "en":
                            return "eng";
                        case "ar":
                            return "ara";
                        default:
                            throw new Exception("language not supported");
                    }
                }
            }

        }
        public class Language
        {
            public string Name { get; set; }
            public long Score { get; set; }
            public string Iso6391Name { get; set; }
        }

        public IList<Document> Documents { get; set; }
    }
}
