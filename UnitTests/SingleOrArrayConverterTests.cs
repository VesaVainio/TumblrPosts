using Microsoft.VisualStudio.TestTools.UnitTesting;
using Model.Tumblr;
using Newtonsoft.Json;

namespace UnitTests
{
    [TestClass]
    public class SingleOrArrayConverterTests
    {
        public static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore
        };

        [TestMethod]
        public void TestSingleMedia()
        {
            string json = "{\"media\":{\"url\":\"foo\"}}";

            Content content = JsonConvert.DeserializeObject<Content>(json);

            Assert.IsNotNull(content.Media);
            Assert.AreEqual(1, content.Media.Length);
            Assert.AreEqual("foo", content.Media[0].Url);

            string output = JsonConvert.SerializeObject(content, SerializerSettings);
            Assert.AreEqual("{\"Media\":[{\"Url\":\"foo\"}]}", output);
        }

        [TestMethod]
        public void TestArrayMedia()
        {
            string json = "{\"media\":[{\"url\":\"foo1\"},{\"url\":\"foo2\"}]}";

            Content content = JsonConvert.DeserializeObject<Content>(json);

            Assert.IsNotNull(content.Media);
            Assert.AreEqual(2, content.Media.Length);
            Assert.AreEqual("foo1", content.Media[0].Url);
            Assert.AreEqual("foo2", content.Media[1].Url);

            string output = JsonConvert.SerializeObject(content, SerializerSettings);
            Assert.AreEqual("{\"Media\":[{\"Url\":\"foo1\"},{\"Url\":\"foo2\"}]}", output);
        }
    }
}