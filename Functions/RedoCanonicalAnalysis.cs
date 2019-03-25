using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Model.Canonical;
using Model.Google;
using Model.Microsoft;
using Model.Site;
using Newtonsoft.Json;
using QueueInterface;
using QueueInterface.Messages;
using TableInterface;
using TableInterface.Entities;

namespace Functions
{
    public static class RedoCanonicalAnalysis
    {
        [FunctionName("RedoCanonicalAnalysis")]
        public static HttpResponseMessage Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "redo-canonical")] HttpRequestMessage req, TraceWriter log)
        {
            Startup.Init();

            ImageAnalysisTableAdapter imageAnalysisTableAdapter = new ImageAnalysisTableAdapter();
            imageAnalysisTableAdapter.Init();

            List<ImageAnalysisEntity> analyses = imageAnalysisTableAdapter.GetAll();

            foreach (ImageAnalysisEntity entity in analyses)
            {
                Response visionResponse = JsonConvert.DeserializeObject<Response>(entity.GoogleVisionApiJson);
                Analysis msAnalysis = JsonConvert.DeserializeObject<Analysis>(entity.MsAnalysisJson);
                List<Face> msFaces = JsonConvert.DeserializeObject<List<Face>>(entity.MsCognitiveFaceDetectJson);

                ImageAnalysis canonicalImageAnalysis = new ImageAnalysis(visionResponse, msAnalysis, msFaces);

                entity.CanonicalJson = JsonConvert.SerializeObject(canonicalImageAnalysis, JsonUtils.AnalysisSerializerSettings);
                imageAnalysisTableAdapter.UpdateImageAnalysis(entity);
            }

            return req.CreateResponse(HttpStatusCode.OK, $"Updated {analyses.Count} analyses");
        }
    }
}