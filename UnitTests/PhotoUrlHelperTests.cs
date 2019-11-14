using Microsoft.VisualStudio.TestTools.UnitTesting;
using TumblrPics.Model;

namespace UnitTests
{
    [TestClass]
    public class PhotoUrlHelperTests
    {
        private const string newUrl = "https://66.media.tumblr.com/2658176ff018529c2dfbacb57a8a36dc/4edcb8991d3eea7f-00/s1280x1920/7c1f445b8d63b45cb881c7a86eba4459f05d9a96.jpg";
        private const string newSquareUrl = "https://46.media.tumblr.com/a239440e3fa7a99a898401d85197aea0/790fc572931cda8a-78/s400x600_f1/ce2a425b67c9e3605f6545c6c308327af5ca71f4.gif";

        private const string oldUrl = "https://66.media.tumblr.com/2d0b81c6d988de86dcaf9abaaaaab563/tumblr_p738u7LON31tum2j0o1_500.jpg";
        private const string oldSquareUrl = "https://66.media.tumblr.com/f63951217c08428cc3db05cdfa440426/tumblr_ptauwmXZAa2t88716_75sq.jpg";

        [TestMethod]
        public void TestParseOldUrl()
        {
            PhotoUrlHelper helper = PhotoUrlHelper.ParseTumblr(oldUrl);

            Assert.IsNotNull(helper);
            Assert.AreEqual(66, helper.Server);
            Assert.AreEqual(500, helper.Size);
            Assert.AreEqual("2d0b81c6d988de86dcaf9abaaaaab563", helper.Container);
            Assert.AreEqual("p738u7LON31tum2j0o1", helper.Name);
            Assert.AreEqual("jpg", helper.Extension);

            Assert.IsNull(helper.Hash);
            Assert.IsNull(helper.VSize);
        }

        [TestMethod]
        public void TestParseOldSquareUrl()
        {
            PhotoUrlHelper helper = PhotoUrlHelper.ParseTumblr(oldSquareUrl);

            Assert.IsNotNull(helper);
            Assert.AreEqual(66, helper.Server);
            Assert.AreEqual(75, helper.Size);
            Assert.AreEqual("f63951217c08428cc3db05cdfa440426", helper.Container);
            Assert.AreEqual("ptauwmXZAa2t88716", helper.Name);
            Assert.AreEqual("jpg", helper.Extension);

            Assert.IsNull(helper.Hash);
            Assert.IsNull(helper.VSize);
        }



        [TestMethod]
        public void TestParseNewUrl()
        {
            PhotoUrlHelper helper = PhotoUrlHelper.ParseTumblr(newUrl);

            Assert.IsNotNull(helper);
            Assert.AreEqual(66, helper.Server);
            Assert.AreEqual(1280, helper.Size);
            Assert.AreEqual(1920, helper.VSize);
            Assert.AreEqual("2658176ff018529c2dfbacb57a8a36dc", helper.Container);
            Assert.AreEqual("4edcb8991d3eea7f-00", helper.Name);
            Assert.AreEqual("7c1f445b8d63b45cb881c7a86eba4459f05d9a96", helper.Hash);
            Assert.AreEqual("jpg", helper.Extension);
        }

        [TestMethod]
        public void TestParseNewSquareUrl()
        {
            PhotoUrlHelper helper = PhotoUrlHelper.ParseTumblr(newSquareUrl);

            Assert.IsNotNull(helper);
            Assert.AreEqual(46, helper.Server);
            Assert.AreEqual(400, helper.Size);
            Assert.AreEqual(600, helper.VSize);
            Assert.AreEqual("a239440e3fa7a99a898401d85197aea0", helper.Container);
            Assert.AreEqual("790fc572931cda8a-78", helper.Name);
            Assert.AreEqual("ce2a425b67c9e3605f6545c6c308327af5ca71f4", helper.Hash);
            Assert.AreEqual("gif", helper.Extension);
        }
    }
}