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

            Assert.AreEqual(39, tokens.Length);
            Assert.IsTrue(tokens.Any(x => x.Equals("i'm")));
        }

        [TestMethod]
        public void TestStringTokenizer2()
        {
            string[] tokens = StringTokenizer.Tokenize(
                "Thanks for the lotion!)\nNow you can have 5 minutes to find your key\nin the pool unlock your device and play.Better hurryup!\n");

            Assert.AreEqual(24, tokens.Length);
            Assert.IsTrue(tokens.All(x => !x.Contains("!")));
        }
    }
}