using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host;
using Model.Canonical;
using Model.Google;
using Model.Microsoft;
using Newtonsoft.Json;
using QueueInterface.Messages;
using TableInterface;
using TableInterface.Entities;

namespace Functions
{
    public static class ImageAnalyzer
    {
        public static async Task AnalyzePhoto(TraceWriter log, PhotoToAnalyze photoToAnalyze, ImageAnalysisTableAdapter imageAnalysisTableAdapter)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

                string apiKey = ConfigurationManager.AppSettings["GoogleApiKey"];

                string url = "https://vision.googleapis.com/v1/images:annotate?key=" + apiKey;

                VisionApiRequest request = VisionApiRequest.CreateFromImageUris(photoToAnalyze.Url);

                string requestJson = JsonConvert.SerializeObject(request, JsonUtils.GoogleSerializerSettings);

                StringContent stringContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await httpClient.PostAsync(url, stringContent);
                HttpContent responseContent = response.Content;
                string googleVisionResponseString = await responseContent.ReadAsStringAsync();

                VisionApiResponse visionApiResponse =
                    JsonConvert.DeserializeObject<VisionApiResponse>(googleVisionResponseString, JsonUtils.GoogleSerializerSettings);

                log.Info($"Google Vision for {photoToAnalyze.Url} got in {stopwatch.ElapsedMilliseconds}ms");
                stopwatch.Restart();

                string faceApiKey = ConfigurationManager.AppSettings["FaceApiKey"];
                httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", faceApiKey);
                stringContent = new StringContent($"{{\"url\":\"{photoToAnalyze.Url}\"}}", Encoding.UTF8, "application/json");
                response = await httpClient.PostAsync(
                    "https://northeurope.api.cognitive.microsoft.com/face/v1.0/detect?returnFaceId=true&returnFaceLandmarks=false&returnFaceAttributes=" +
                    "age,gender,headPose,smile,facialHair,glasses,emotion,hair,makeup,occlusion,accessories,blur,exposure,noise",
                    stringContent);
                responseContent = response.Content;

                string msDetectResponseString = await responseContent.ReadAsStringAsync();
                List<Face> msFaces = JsonConvert.DeserializeObject<List<Face>>(msDetectResponseString);

                log.Info($"MS Faces for {photoToAnalyze.Url} got in {stopwatch.ElapsedMilliseconds}ms");
                stopwatch.Restart();

                string visionApiKey = ConfigurationManager.AppSettings["ComputerVisionApiKey"];
                httpClient.DefaultRequestHeaders.Remove("Ocp-Apim-Subscription-Key");
                httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", visionApiKey);
                stringContent = new StringContent($"{{\"url\":\"{photoToAnalyze.Url}\"}}", Encoding.UTF8, "application/json");
                response = await httpClient.PostAsync(
                    "https://northeurope.api.cognitive.microsoft.com/vision/v2.0/analyze?visualFeatures=Description,ImageType,Adult,Categories,Tags,Objects,Color&language=en",
                    stringContent);
                responseContent = response.Content;

                string msAnalyzeResponseString = await responseContent.ReadAsStringAsync();
                Analysis msAnalysis = JsonConvert.DeserializeObject<Analysis>(msAnalyzeResponseString);

                log.Info($"MS Analysis for {photoToAnalyze.Url} got in {stopwatch.ElapsedMilliseconds}ms");
                stopwatch.Restart();

                if (visionApiResponse.Responses.Count == 1 && msAnalysis != null)
                {
                    ImageAnalysis canonicalImageAnalysis = new ImageAnalysis(visionApiResponse.Responses[0], msAnalysis, msFaces);
                    ImageAnalysisEntity imageAnalysisEntity = new ImageAnalysisEntity
                    {
                        // for canonical truncate decimal precision to 4 decimal places, for others keep original precision
                        CanonicalJson = JsonConvert.SerializeObject(canonicalImageAnalysis, JsonUtils.AnalysisSerializerSettings),
                        GoogleVisionApiJson = JsonConvert.SerializeObject(visionApiResponse.Responses[0], JsonUtils.JsonSerializerSettings),
                        MsCognitiveFaceDetectJson = JsonConvert.SerializeObject(msFaces, JsonUtils.JsonSerializerSettings)
                    };
                    imageAnalysisTableAdapter.InsertImageAnalysis(imageAnalysisEntity, photoToAnalyze.Url);
                    imageAnalysisTableAdapter.InsertBlogImageAnalysis(SanitizeSourceBlog(photoToAnalyze.Blog), photoToAnalyze.Url);
                    log.Info($"All analyses for {photoToAnalyze.Url} saved in {stopwatch.ElapsedMilliseconds}ms");
                }
                else
                {
                    log.Warning("Failed to get all responses");
                }
            }
        }

        private static string SanitizeSourceBlog(string sourceBlog)
        {
            return sourceBlog?.Replace(' ', '_').Replace('/', '_').Replace("\\", "_").Replace('?', '_').Replace('#', '_');
        }
    }
}