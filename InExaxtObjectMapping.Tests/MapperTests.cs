using Microsoft.VisualStudio.TestTools.UnitTesting;
using InExaxtObjectMapping.Tests.SampleTypes;
using System;
using InExactObjectMapping;

namespace InExaxtObjectMapping.Tests
{
    [TestClass]
    public class MapperTests
    {
        [TestMethod]
        public void CopyMatchingPropertiestTest()
        {
            var randomType = new RandomType(0, Guid.NewGuid().ToString(), 0, Guid.NewGuid().ToString());
            var randomTypeSister = new RandomTypeSister();

            randomType.CopyValuesFromMatchingPropertiesToSisterType(randomTypeSister);

            Assert.AreEqual(randomType.MatchedProperty1, randomTypeSister.MatchedProperty1);
            Assert.AreEqual(randomType.MatchedProperty2, randomTypeSister.MatchedProperty2);
        }
    }
}
