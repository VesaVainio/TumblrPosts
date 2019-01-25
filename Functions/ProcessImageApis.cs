using System.Configuration;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Model.Google;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Functions
{
    public static class ProcessImageApis
    {
        private static readonly DefaultContractResolver ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new CamelCaseNamingStrategy()
        };

        public static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            ContractResolver = ContractResolver
        };

        [FunctionName("ProcessImageApis")]
        public static async Task Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "processimage")]HttpRequestMessage req, TraceWriter log)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

                string apiKey = ConfigurationManager.AppSettings["GoogleApiKey"];

                string url = "https://vision.googleapis.com/v1/images:annotate?key=" + apiKey;

                VisionApiRequest request =
                    VisionApiRequest.CreateFromImageUris(
                        "https://tumblrpics.blob.core.windows.net/orig-66/c48440825ac6eab1af6c4de39bbc59d6_ph8zkhbeVA1tf8706_1280.jpg");

                string requestJson = JsonConvert.SerializeObject(request, JsonSerializerSettings);

                StringContent stringContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await httpClient.PostAsync(url, stringContent);
                //if (response.IsSuccessStatusCode)
                //{
                    HttpContent responseContent = response.Content;
                    string responseContentString = await responseContent.ReadAsStringAsync();
                //}
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