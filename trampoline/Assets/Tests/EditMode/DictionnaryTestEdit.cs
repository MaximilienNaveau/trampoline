using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace trampoline.tests
{
    [TestFixture]
    public class DictionaryTestEdit
    {
        private FrenchDictionary obj_ = new FrenchDictionary();

        [OneTimeSetUp]
        public void RunBeforeAnyTests()
        {
            obj_.initialize(/*async = */ false);
        }

        [OneTimeTearDown]
        public void RunAfterAnyTests()
        {
        }

        public static string GenerateAlphaRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            System.Random random = new System.Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static string GenerateAlphaNumericRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            const string numbers = "0123456789";
            System.Random random = new System.Random();

            // Ensure at least one number is included
            string result = new string(Enumerable.Repeat(chars, length - 1)
                .Select(s => s[random.Next(s.Length)]).ToArray());

            // Add a random number at a random position
            int numberPosition = random.Next(length);
            char randomNumber = numbers[random.Next(numbers.Length)];
            result = result.Insert(numberPosition, randomNumber.ToString());

            return result;
        }

        [Test]
        public async Task TestLoadAsyncDictionary()
        {
            var obj = new FrenchDictionary();
            Assert.AreEqual(obj.isLoaded(), false);
            obj.initialize(/*async*/ true);
            int timout = 0;
            while (!obj.isLoaded() && timout < 100)
            {
                await Task.Delay(100);
                timout++;
            }
            Assert.AreEqual(obj.isLoaded(), true);
        }

        [Test]
        public void TestLoadDictionary()
        {
            Assert.AreEqual(obj_.isLoaded(), true);
        }


        [Test]
        public void TestAllWordsAreValid()
        {
            // Ensure the dictionary is loaded
            Assert.IsTrue(obj_.isLoaded());

            // Verify that all words in the dictionary are valid
            foreach (var word in obj_.GetWords())
            {
                obj_.isWordValid(word);
            }
        }

        [Test]
        public void TestNormalizeAccents()
        {
            Assert.AreEqual(FrenchDictionary.NormalizeWord("é"), "E");
            Assert.AreEqual(FrenchDictionary.NormalizeWord("è"), "E");
            Assert.AreEqual(FrenchDictionary.NormalizeWord("ê"), "E");
            Assert.AreEqual(FrenchDictionary.NormalizeWord("ë"), "E");
            Assert.AreEqual(FrenchDictionary.NormalizeWord("à"), "A");
            Assert.AreEqual(FrenchDictionary.NormalizeWord("â"), "A");
            Assert.AreEqual(FrenchDictionary.NormalizeWord("ä"), "A");
            Assert.AreEqual(FrenchDictionary.NormalizeWord("ù"), "U");
            Assert.AreEqual(FrenchDictionary.NormalizeWord("û"), "U");
            Assert.AreEqual(FrenchDictionary.NormalizeWord("ü"), "U");
            Assert.AreEqual(FrenchDictionary.NormalizeWord("î"), "I");
            Assert.AreEqual(FrenchDictionary.NormalizeWord("ï"), "I");
            Assert.AreEqual(FrenchDictionary.NormalizeWord("ô"), "O");
            Assert.AreEqual(FrenchDictionary.NormalizeWord("ö"), "O");
            Assert.AreEqual(FrenchDictionary.NormalizeWord("ç"), "C");
            Assert.AreEqual(FrenchDictionary.NormalizeWord("ÿ"), "Y");
            Assert.AreEqual(FrenchDictionary.NormalizeWord("œ"), "OE");
            Assert.AreEqual(FrenchDictionary.NormalizeWord("æ"), "AE");
        }

        [Test]
        public void TestContainsNumber()
        {
            Assert.IsTrue(FrenchDictionary.ContainsNumber(
                GenerateAlphaNumericRandomString(9)));
            Assert.IsTrue(FrenchDictionary.ContainsNumber(
                GenerateAlphaNumericRandomString(9)));
            Assert.IsTrue(FrenchDictionary.ContainsNumber(
                GenerateAlphaNumericRandomString(9)));
            Assert.IsTrue(FrenchDictionary.ContainsNumber(
                GenerateAlphaNumericRandomString(9)));
            Assert.IsTrue(FrenchDictionary.ContainsNumber(
                GenerateAlphaNumericRandomString(9)));
            Assert.IsTrue(FrenchDictionary.ContainsNumber(
                GenerateAlphaNumericRandomString(9)));
            Assert.IsTrue(FrenchDictionary.ContainsNumber(
                GenerateAlphaNumericRandomString(9)));
            Assert.IsTrue(FrenchDictionary.ContainsNumber(
                GenerateAlphaNumericRandomString(9)));
            Assert.IsTrue(FrenchDictionary.ContainsNumber(
                GenerateAlphaNumericRandomString(9)));
            Assert.IsFalse(FrenchDictionary.ContainsNumber(
                GenerateAlphaRandomString(9)));
            Assert.IsFalse(FrenchDictionary.ContainsNumber(
                GenerateAlphaRandomString(9)));
            Assert.IsFalse(FrenchDictionary.ContainsNumber(
                GenerateAlphaRandomString(9)));
            Assert.IsFalse(FrenchDictionary.ContainsNumber(
                GenerateAlphaRandomString(9)));
            Assert.IsFalse(FrenchDictionary.ContainsNumber(
                GenerateAlphaRandomString(9)));
            Assert.IsFalse(FrenchDictionary.ContainsNumber(
                GenerateAlphaRandomString(9)));
            Assert.IsFalse(FrenchDictionary.ContainsNumber(
                GenerateAlphaRandomString(9)));
            Assert.IsFalse(FrenchDictionary.ContainsNumber(
                GenerateAlphaRandomString(9)));
            Assert.IsFalse(FrenchDictionary.ContainsNumber(
                GenerateAlphaRandomString(9)));
        }

        [Test]
        public void TestValidA()
        {
            Assert.IsTrue(obj_.isWordValid("A"));
        }

        [Test]
        public void TestValidSmallA()
        {
            Assert.IsFalse(obj_.isWordValid("a"));
        }

        [Test]
        public void TestValidCA()
        {
            Assert.AreEqual(FrenchDictionary.NormalizeWord("ça"), "CA");
            Assert.IsTrue(obj_.isWordValid("CA"));
        }

        [Test]
        public void TestValidEte()
        {
            Assert.IsTrue(obj_.isWordValid("ETE"));
        }

        [Test]
        public void TestValidCafe()
        {
            Assert.IsTrue(obj_.isWordValid("CAFE"));
        }

        [Test]
        public void TestValidElement()
        {
            Assert.IsTrue(obj_.isWordValid("ELEMENT"));
        }

        [Test]
        public void TestValidWordProperNounPARIS()
        {
            Assert.IsTrue(obj_.isWordValid("PARIS"));
        }

        [Test]
        public void TestValidWordProperNounEDIMBOURG()
        {
            Assert.IsTrue(obj_.isWordValid("EDIMBOURG"));
        }

        [Test]
        public void TestValidWordProperNounFRANCOIS()
        {
            Assert.IsTrue(obj_.isWordValid("FRANCOIS"));
        }

        [Test]
        public void TestValidWordProperNounOEUVRER()
        {
            Assert.IsTrue(obj_.isWordValid("OEUVRER"));
        }
    }
}