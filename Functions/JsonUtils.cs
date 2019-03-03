using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Functions
{
    public class JsonUtils
    {
        public static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings {NullValueHandling = NullValueHandling.Ignore};

        private static readonly DefaultContractResolver ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new CamelCaseNamingStrategy()
        };

        public static readonly JsonSerializerSettings GoogleSerializerSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            ContractResolver = ContractResolver
        };
    }
}