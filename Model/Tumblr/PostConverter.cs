using System;
using Newtonsoft.Json;

namespace Model.Tumblr
{
    // TODO: work in progress, class could be modified to convert problematic json, but not really used at the moment...
    public class PostConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            Post post = new Post();
            serializer.Populate(reader, post);
            return post;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Post);
        }

        public override bool CanRead => true;
        public override bool CanWrite => false;
    }
}