using System;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Functions
{
    public class JsonUtils
    {
        public static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
        public static readonly JsonSerializerSettings AnalysisSerializerSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

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

        static JsonUtils()
        {
            AnalysisSerializerSettings.Converters.Add(new DecimalJsonConverter());
        }
    }

    internal class DecimalJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(decimal);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteRawValue(((decimal) value).ToString("0.####", CultureInfo.InvariantCulture));
        }
    }
}