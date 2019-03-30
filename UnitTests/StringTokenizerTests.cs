using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Model;

namespace UnitTests
{
    [TestClass]
    public class StringTokenizerTests
    {
        [TestMethod]
        public void TestStringTokenizer1()
        {
            string[] tokens = StringTokenizer.Tokenize(
                "Why Do I Tease & Deny\nYou And Give You\nA Hard Time\nEveryday ???\nBecause It Builds\nYour Character My\nStudmuffin...\n" +
                "I'm Not Only Training\nYour Mind How\nNOT to Complain,\nI'm Also Programming\nYour Mind My Darling\nWith Dopamine...\n");

            Assert.AreEqual(42, tokens.Length);
            Assert.IsTrue(tokens.Any(x => x.Equals("i'm")));
            Assert.AreEqual(3, tokens.Count(x => x == "."));
        }

        [TestMethod]
        public void TestStringTokenizer2()
        {
            string[] tokens = StringTokenizer.Tokenize(
                "Thanks for the lotion!)\nNow you can have 5 minutes to find your key\nin the pool unlock your device and play.Better hurryup!\n");

            Assert.AreEqual(27, tokens.Length);
            Assert.IsTrue(tokens.All(x => !x.Contains("!")));
            Assert.AreEqual(3, tokens.Count(x => x == "."));
        }

        [TestMethod]
        public void TestGetDigrams()
        {
            string[] digrams = StringTokenizer.GetDigrams(new string[]
                { "c'mon", "tell", "me", "all", "your", "secrets", ".", "i", "won't", "tell", "anyone", "." });

            Assert.AreEqual(8, digrams.Length);
            Assert.IsTrue(digrams.All(x => !x.Contains(".")));
            Assert.IsTrue(digrams.All(x => x != "secrets i"));
            Assert.IsTrue(digrams.Any(x => x != "c'mon tell"));
            Assert.IsTrue(digrams.Any(x => x != "tell anyone"));
        }

        [TestMethod]
        public void SanitizeTableKeyTest()
        {
            Assert.AreEqual("Picture 5", StringTokenizer.SanitizeTableKey("Picture #5", ""));
        }
    }
}