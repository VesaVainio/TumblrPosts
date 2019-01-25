using Newtonsoft.Json;

namespace Functions
{
    public class JsonUtils
    {
        public static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings {NullValueHandling = NullValueHandling.Ignore};
    }
}