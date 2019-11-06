using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using static LanguageExt.Prelude;
using static RVis.Model.Test.DistributionImpl;
using static System.Double;

namespace RVis.Model.Test
{
  [TestClass()]
  public class UnitTestUniformDistribution
  {
#if !IS_PIPELINES_BUILD
    [TestMethod()]
    public void TestUniformCumulativeDistributionAtBounds()
    {
      // arrange
      var subject = new UniformDistribution(0.1, 0.9);
      var expectedLowerP = GetNumData("punif(0.1, 0.1, 0.9)")[0].Data[0];
      var expectedUpperP = GetNumData("punif(0.9, 0.1, 0.9)")[0].Data[0];

      // act
      var (actualLowerP, actualUpperP) = subject.CumulativeDistributionAtBounds;

      // assert
      Assert.AreEqual(expectedLowerP, actualLowerP, Base.Constant.TOLERANCE);
      Assert.AreEqual(expectedUpperP, actualUpperP, Base.Constant.TOLERANCE);
    }
#endif

    [TestMethod()]
    public void TestUniformWithBoundsFillSamples()
    {
      // arrange
      var subject = new UniformDistribution(0.1, 0.9);
      var samples = Range(0, 42).Map(_ => NaN).ToArray();

      // act
      subject.FillSamples(samples);

      // assert
      Assert.IsTrue(samples.All(d => d > 0.1 && d < 0.9));
    }

    [TestMethod()]
    public void TestUniformWithBoundsGetSample()
    {
      // arrange
      var subject = new UniformDistribution(0.1, 0.9);

      // act
      var sample = subject.GetSample();

      // assert
      Assert.IsTrue(sample > 0.1 && sample < 0.9);
    }

#if !IS_PIPELINES_BUILD
    [TestMethod()]
    public void TestUniformGetDensities()
    {
      // arrange
      var subject = new UniformDistribution(0.1, 0.9);
      var expected = GetNumData(
        "sapply(seq(qunif(0.3, 0.1, 0.9), qunif(0.7, 0.1, 0.9), length.out = 5), function(cd){dunif(cd, 0.1, 0.9)})"
        )[0].Data.ToArr();

      // act
      var (_, actual) = subject.GetDensities(0.3, 0.7, 5);

      // assert
      Assert.IsTrue(actual.Count == 5);
      expected.Iter((i, d) => Assert.AreEqual(d, actual[i], Base.Constant.TOLERANCE));
    }
#endif

    [TestMethod()]
    public void TestUniformRoundTrip()
    {
      // arrange
      var expected = new UniformDistribution(0.1, 0.9);

      // act
      var serialized = expected.ToString();
      var deserialized = Distribution.DeserializeDistribution(serialized);
      var distribution = deserialized.IfNone(() => { Assert.Fail(); return default; });

      // assert
      var actual = Base.Check.RequireInstanceOf<UniformDistribution>(distribution);
      Assert.AreEqual(expected, actual);
    }
  }
}