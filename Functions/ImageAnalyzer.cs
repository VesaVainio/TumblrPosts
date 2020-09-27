using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host;
using Model;
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
        public static async Task<string> AnalyzePhoto(TraceWriter log, PhotoToAnalyze photoToAnalyze, ImageAnalysisTableAdapter imageAnalysisTableAdapter)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

                VisionApiResponse visionApiResponse = await GetGoogleVisionApi(log, photoToAnalyze, httpClient);
                List<Face> msFaces = await GetMsFaces(log, photoToAnalyze, httpClient);
                Analysis msAnalysis = await GetMsAnalysis(log, photoToAnalyze, httpClient);

                if (visionApiResponse?.Responses.Count == 1 && msAnalysis != null)
                {
                    ImageAnalysis canonicalImageAnalysis = new ImageAnalysis(visionApiResponse.Responses[0], msAnalysis, msFaces);
                    ImageAnalysisEntity imageAnalysisEntity = new ImageAnalysisEntity
                    {
                        // for canonical truncate decimal precision to 4 decimal places, for others keep original precision
                        CanonicalJson = JsonConvert.SerializeObject(canonicalImageAnalysis, JsonUtils.AnalysisSerializerSettings),
                        GoogleVisionApiJson = JsonConvert.SerializeObject(visionApiResponse.Responses[0], JsonUtils.JsonSerializerSettings),
                        MsCognitiveFaceDetectJson = JsonConvert.SerializeObject(msFaces, JsonUtils.JsonSerializerSettings),
                        MsAnalysisJson = JsonConvert.SerializeObject(msAnalysis, JsonUtils.JsonSerializerSettings)
                    };

                    if (imageAnalysisEntity.GoogleVisionApiJson.Length > 30000)
                    {
                        log.Warning($"Google vision API response JSON is {imageAnalysisEntity.GoogleVisionApiJson.Length} chars, removing TextAnnotations");
                        visionApiResponse.Responses[0].TextAnnotations = null;
                        imageAnalysisEntity.GoogleVisionApiJson = JsonConvert.SerializeObject(visionApiResponse.Responses[0], JsonUtils.JsonSerializerSettings);
                    }

                    if (imageAnalysisEntity.GoogleVisionApiJson.Length > 45000)
                    {
                        log.Warning($"GoogleVisionApiJson still is {imageAnalysisEntity.GoogleVisionApiJson.Length} chars after removing TextAnnotations");
                    }

                    if (imageAnalysisEntity.CanonicalJson.Length > 45000)
                    {
                        log.Warning($"CanonicalJson is {imageAnalysisEntity.CanonicalJson.Length} chars");
                    }
                    if (imageAnalysisEntity.MsCognitiveFaceDetectJson.Length > 45000)
                    {
                        log.Warning($"MsCognitiveFaceDetectJson is {imageAnalysisEntity.MsCognitiveFaceDetectJson.Length} chars");
                    }
                    if (imageAnalysisEntity.MsAnalysisJson.Length > 45000)
                    {
                        log.Warning($"MsAnalysisJson is {imageAnalysisEntity.MsAnalysisJson.Length} chars");
                    }

                    imageAnalysisTableAdapter.InsertImageAnalysis(imageAnalysisEntity, photoToAnalyze.Url);
                    imageAnalysisTableAdapter.InsertBlogImageAnalysis(SanityHelper.SanitizeSourceBlog(photoToAnalyze.Blog), photoToAnalyze.Url);
                    log.Info($"All analyses for {photoToAnalyze.Url} saved in {stopwatch.ElapsedMilliseconds}ms");
                    return null;
                }

                log.Warning("Failed to get all responses");
                return "Failed to get all responses";
            }
        }

        private static async Task<VisionApiResponse> GetGoogleVisionApi(TraceWriter log, PhotoToAnalyze photoToAnalyze, HttpClient httpClient)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            string apiKey = ConfigurationManager.AppSettings["GoogleApiKey"];

            string url = "https://vision.googleapis.com/v1/images:annotate?key=" + apiKey;

            VisionApiRequest request = VisionApiRequest.CreateFromImageUris(photoToAnalyze.Url);

            VisionApiResponse visionApiResponse = await MakeVisionApiRequest(httpClient, request, url, log);

            if (visionApiResponse?.Responses[0].Error != null)
            {
                if (visionApiResponse.Responses[0].Error.Message.Contains("download the content and pass it in"))
                {
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/*"));
                    log.Info($"Google API error, downloading content for {photoToAnalyze.Url}");
                    byte[] photoBytes = await httpClient.GetByteArrayAsync(photoToAnalyze.Url);
                    log.Info($"Got {photoBytes.Length} bytes, making Google API request with Content");
                    request = VisionApiRequest.CreateFromContent(photoBytes);
                    visionApiResponse = await MakeVisionApiRequest(httpClient, request, url, log);
                }

                if (visionApiResponse?.Responses[0].Error != null)
                {
                    string failure = $"Got error response from Google API: {visionApiResponse.Responses[0].Error.Message}";
                    log.Warning(failure);
                    throw new InvalidOperationException(failure);
                }
            }

            log.Info($"Google Vision for {photoToAnalyze.Url} got in {stopwatch.ElapsedMilliseconds}ms");
            stopwatch.Restart();
            return visionApiResponse;
        }

        private static async Task<VisionApiResponse> MakeVisionApiRequest(HttpClient httpClient, VisionApiRequest request, string url, TraceWriter log)
        {
            string requestJson = JsonConvert.SerializeObject(request, JsonUtils.GoogleSerializerSettings);

            StringContent stringContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await httpClient.PostAsync(url, stringContent);
            HttpContent responseContent = response.Content;
            string googleVisionResponseString = await responseContent.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                log.Error("Google Vision API request failed: " + googleVisionResponseString);
                throw new InvalidOperationException("Google Vision API request failed");
            }

            VisionApiResponse visionApiResponse =
                JsonConvert.DeserializeObject<VisionApiResponse>(googleVisionResponseString, JsonUtils.GoogleSerializerSettings);
            return visionApiResponse;
        }

        private static async Task<List<Face>> GetMsFaces(TraceWriter log, PhotoToAnalyze photoToAnalyze, HttpClient httpClient)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            string faceApiKey = ConfigurationManager.AppSettings["FaceApiKey"];
            httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", faceApiKey);
            StringContent stringContent = new StringContent($"{{\"url\":\"{photoToAnalyze.Url}\"}}", Encoding.UTF8, "application/json");
            HttpResponseMessage response = await httpClient.PostAsync(
                "https://northeurope.api.cognitive.microsoft.com/face/v1.0/detect?returnFaceId=true&returnFaceLandmarks=false&returnFaceAttributes=" +
                "age,gender,headPose,smile,facialHair,glasses,emotion,hair,makeup,occlusion,accessories,blur,exposure,noise",
                stringContent);
            HttpContent responseContent = response.Content;

            string msDetectResponseString = await responseContent.ReadAsStringAsync();

            List<Face> msFaces;
            try
            {
                msFaces = JsonConvert.DeserializeObject<List<Face>>(msDetectResponseString);
            }
            catch (Exception ex)
            {
                string failure = $"Error while trying to deserialize MS detect response. Response string was: \"{msDetectResponseString}\"";
                log.Error(failure, ex);
                throw;
            }

            log.Info($"MS Faces for {photoToAnalyze.Url} got in {stopwatch.ElapsedMilliseconds}ms");
            stopwatch.Restart();
            return msFaces;
        }

        private static async Task<Analysis> GetMsAnalysis(TraceWriter log, PhotoToAnalyze photoToAnalyze, HttpClient httpClient)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            string visionApiKey = ConfigurationManager.AppSettings["ComputerVisionApiKey"];
            httpClient.DefaultRequestHeaders.Remove("Ocp-Apim-Subscription-Key");
            httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", visionApiKey);
            StringContent stringContent = new StringContent($"{{\"url\":\"{photoToAnalyze.Url}\"}}", Encoding.UTF8, "application/json");
            HttpResponseMessage response = await httpClient.PostAsync(
                "https://northeurope.api.cognitive.microsoft.com/vision/v2.0/analyze?visualFeatures=Description,ImageType,Adult,Categories,Tags,Objects,Color&language=en",
                stringContent);
            HttpContent responseContent = response.Content;

            string msAnalyzeResponseString = await responseContent.ReadAsStringAsync();
            Analysis msAnalysis = JsonConvert.DeserializeObject<Analysis>(msAnalyzeResponseString);

            log.Info($"MS Analysis for {photoToAnalyze.Url} got in {stopwatch.ElapsedMilliseconds}ms");
            return msAnalysis;
        }
    }
}