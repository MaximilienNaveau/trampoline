using System;
using System.Collections.Generic;
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
            obj_.initialize(/*async = false*/);
        }

        [OneTimeTearDown]
        public void RunAfterAnyTests()
        {
        }

        [Test]
        public async Task TestLoadAsyncDictionary()
        {
            var obj = new FrenchDictionary();
            Assert.AreEqual(obj.isLoaded(), false);
            await obj.initializeAsync(/*async = true*/);
            Assert.AreEqual(obj.isLoaded(), true);
        }

        [Test]
        public void TestLoadDictionary()
        {
            Assert.AreEqual(obj_.isLoaded(), true);
        }

        [Test]
        public void TestValidA()
        {
            Assert.AreEqual(obj_.isWordValid("A"), true);
        }

        [Test]
        public void TestValidCA()
        {
            Assert.AreEqual(obj_.isWordValid("CA"), true);
        }

        [Test]
        public void TestValidEte()
        {
            Assert.AreEqual(obj_.isWordValid("ETE"), true);
        }

        [Test]
        public void TestValidCafe()
        {
            Assert.AreEqual(obj_.isWordValid("CAFE"), true);
        }

        [Test]
        public void TestValidElement()
        {
            Assert.AreEqual(obj_.isWordValid("ELEMENT"), true);
        }

        [Test]
        public void TestValidWordProperNounPARIS()
        {
            Assert.AreEqual(obj_.isWordValid("PARIS"), true);
        }

        [Test]
        public void TestValidWordProperNounEDINBOURGH()
        {
            Assert.AreEqual(obj_.isWordValid("EDINBOURGH"), true);
        }

        [Test]
        public void TestValidWordProperNounFRANCOIS()
        {
            Assert.AreEqual(obj_.isWordValid("FRANCOIS"), true);
        }

        [Test]
        public void TestValidWordProperNounOEUVRER()
        {
            Assert.AreEqual(obj_.isWordValid("OEUVRER"), true);
        }
    }
}