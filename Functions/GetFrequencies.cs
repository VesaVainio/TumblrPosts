using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Model;
using Model.Canonical;
using Newtonsoft.Json;
using TableInterface;
using TableInterface.Entities;

namespace Functions
{
    public static class GetFrequencies
    {
        [FunctionName("GetFrequencies")]
        public static HttpResponseMessage Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "getfrequencies")]
            HttpRequestMessage req, TraceWriter log)
        {
            Startup.Init();

            ImageAnalysisTableAdapter imageAnalysisTableAdapter = new ImageAnalysisTableAdapter();
            imageAnalysisTableAdapter.Init();

            TokenAllocationTableAdapter tokenAllocationTableAdapter = new TokenAllocationTableAdapter();
            tokenAllocationTableAdapter.Init();

            List<ImageAnalysisEntity> analyses = imageAnalysisTableAdapter.GetAllCanonical();

            Dictionary<string, int> digramFrequencies = new Dictionary<string, int>();
            Dictionary<string, int> labelFrequencies = new Dictionary<string, int>();

            int processedCount = 0;

            foreach (ImageAnalysisEntity entity in analyses)
            {
                ImageAnalysis canonicalAnalysis = JsonConvert.DeserializeObject<ImageAnalysis>(entity.CanonicalJson);

                UpdateCounts(StringTokenizer.GetDigrams(canonicalAnalysis.TokenizedText), digramFrequencies);
                UpdateCounts(canonicalAnalysis.Labels.Keys.Distinct(), labelFrequencies);
                processedCount++;
                if (processedCount % 100 == 0)
                {
                    log.Info($"Processed frequencies for {processedCount} image analyses");
                }
            }

            log.Info($"Inserting {digramFrequencies.Count} digrams");
            tokenAllocationTableAdapter.InsertFrequencies(TokenAllocationTableAdapter.PartitionDigram, digramFrequencies);

            log.Info($"Inserting {labelFrequencies.Count} labels");
            tokenAllocationTableAdapter.InsertFrequencies(TokenAllocationTableAdapter.PartitionLabel, labelFrequencies);

            return req.CreateResponse(HttpStatusCode.OK, $"Processed {analyses.Count} analyses");
        }

        private static void UpdateCounts(IEnumerable<string> keys, Dictionary<string, int> labelFrequencies)
        {
            foreach (string label in keys)
            {
                if (labelFrequencies.TryGetValue(label, out int count))
                {
                    labelFrequencies[label] = count + 1;
                }
                else
                {
                    labelFrequencies[label] = 1;
                }
            }
        }
    }
}