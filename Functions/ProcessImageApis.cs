using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Model.Canonical;
using Model.Google;
using Model.Microsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using TableInterface;

namespace Functions
{
    public static class ProcessImageApis
    {
        [FunctionName("ProcessImageApis")]
        public static async Task Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "processimage")]
            HttpRequestMessage req, TraceWriter log)
        {
            Startup.Init();

            ImageAnalysisTableAdapter imageAnalysisTableAdapter = new ImageAnalysisTableAdapter();
            imageAnalysisTableAdapter.Init();

            const string imageUrl = "https://tumblrpics.blob.core.windows.net/orig-66/c48440825ac6eab1af6c4de39bbc59d6_ph8zkhbeVA1tf8706_1280.jpg";

            using (HttpClient httpClient = new HttpClient())
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

                string apiKey = ConfigurationManager.AppSettings["GoogleApiKey"];

                string url = "https://vision.googleapis.com/v1/images:annotate?key=" + apiKey;

                VisionApiRequest request = VisionApiRequest.CreateFromImageUris(imageUrl);

                string requestJson = JsonConvert.SerializeObject(request, JsonUtils.GoogleSerializerSettings);

                StringContent stringContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await httpClient.PostAsync(url, stringContent);
                HttpContent responseContent = response.Content;
                string googleVisionResponseString = await responseContent.ReadAsStringAsync();

                VisionApiResponse visionApiResponse =
                    JsonConvert.DeserializeObject<VisionApiResponse>(googleVisionResponseString, JsonUtils.GoogleSerializerSettings);

                string faceApiKey = ConfigurationManager.AppSettings["FaceApiKey"];
                httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", faceApiKey);
                stringContent = new StringContent($"{{\"url\":\"{imageUrl}\"}}", Encoding.UTF8, "application/json");
                response = await httpClient.PostAsync(
                    "https://northeurope.api.cognitive.microsoft.com/face/v1.0/detect?returnFaceId=true&returnFaceLandmarks=false&returnFaceAttributes=" +
                    "age,gender,headPose,smile,facialHair,glasses,emotion,hair,makeup,occlusion,accessories,blur,exposure,noise",
                    stringContent);
                responseContent = response.Content;

                string msDetectResponseString = await responseContent.ReadAsStringAsync();
                List<Face> msFaces = JsonConvert.DeserializeObject<List<Face>>(msDetectResponseString);

                string visionApiKey = ConfigurationManager.AppSettings["ComputerVisionApiKey"];
                httpClient.DefaultRequestHeaders.Remove("Ocp-Apim-Subscription-Key");
                httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", visionApiKey);
                stringContent = new StringContent($"{{\"url\":\"{imageUrl}\"}}", Encoding.UTF8, "application/json");
                response = await httpClient.PostAsync(
                    "https://northeurope.api.cognitive.microsoft.com/vision/v2.0/analyze?visualFeatures=Description,ImageType,Adult,Categories,Tags,Objects,Color&language=en",
                    stringContent);
                responseContent = response.Content;

                string msAnalyzeResponseString = await responseContent.ReadAsStringAsync();
                Analysis msAnalysis = JsonConvert.DeserializeObject<Analysis>(msAnalyzeResponseString);

                if (visionApiResponse.Responses.Count == 1 && msFaces.Count > 0 && msAnalysis != null)
                {
                    ImageAnalysis canonicalImageAnalysis = new ImageAnalysis(visionApiResponse.Responses[0], msAnalysis, msFaces);
                }
            }
        }
    }
}


//POST https://northeurope.api.cognitive.microsoft.com/face/v1.0/detect?returnFaceId=true&returnFaceLandmarks=false&returnFaceAttributes=age,gender,headPose,smile,facialHair,glasses,emotion,hair,makeup,occlusion,accessories,blur,exposure,noise HTTP/1.1
//Host: northeurope.api.cognitive.microsoft.com
//Content-Type: application/json
//Ocp-Apim-Subscription-Key: ••••••••••••••••••••••••••••••••

//{
//"url": "https://tumblrpics.blob.core.windows.net/orig-66/933cc1593a75370f541f03e1d58f3e34_phqiw3jUdc1tjcrt3_1280.jpg"
//}


//POST https://northeurope.api.cognitive.microsoft.com/vision/v1.0/analyze?visualFeatures=ImageType,Faces,Adult,Categories,Color,Tags,Description&language=en HTTP/1.1
//Host: northeurope.api.cognitive.microsoft.com
//Content-Type: application/json
//Ocp-Apim-Subscription-Key: ••••••••••••••••••••••••••••••••

//{"url":"https://tumblrpics.blob.core.windows.net/orig-66/c48440825ac6eab1af6c4de39bbc59d6_ph8zkhbeVA1tf8706_1280.jpg"}