using Microsoft.VisualStudio.TestTools.UnitTesting;
using RVis.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Math;
using static System.Convert;
using RVis.Base.Extensions;

namespace RVis.Model.Test
{
  [TestClass()]
  public class UnitTestDistribution
  {
    [TestMethod()]
    public void TestGetDistributionTypes()
    {
      // arrange
      var expected = ToInt32(Log((int)DistributionType.All + 1) / Log(2d));

      // act
      var actual = Distribution.GetDistributionTypes(DistributionType.All).Count;

      // assert
      Assert.AreEqual(expected, actual);
    }

    [TestMethod()]
    public void TestSerializeDistributions()
    {
      // arrange
      var instances = Distribution.GetDefaults();

      // act
      var serialized = Distribution.SerializeDistributions(instances);

      // assert
      Assert.IsTrue(serialized.Length == instances.Count);
    }

    [TestMethod()]
    public void TestDeserializeDistributions()
    {
      // arrange
      var instances = Distribution.GetDefaults();
      var serialized = Distribution.SerializeDistributions(instances);

      // act
      var deserialized = Distribution.DeserializeDistributions(serialized);

      // assert
      Assert.IsTrue(instances.SequenceEqual(deserialized));
    }

    [TestMethod()]
    public void TestGetDefault()
    {
      // arrange
      var expected = NormalDistribution.Default;

      // act
      var actual = Distribution.GetDefault(DistributionType.Normal);

      // assert
      Assert.AreEqual(expected, actual);
    }

    [TestMethod()]
    public void GetDefaultsTest()
    {
      // arrange
      var expectedCount = Distribution.GetDistributionTypes(DistributionType.All).Count;

      // act
      var defaults = Distribution.GetDefaults();
      var actualCount = defaults.Count;

      // assert
      Assert.AreEqual(expectedCount, actualCount);
      Assert.IsTrue(defaults.AllUnique(d => d.GetType()));
    }
  }
}