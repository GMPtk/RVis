using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;
using static LanguageExt.Prelude;
using static RVis.Model.Test.DistributionImpl;
using static System.Double;
using static System.Math;

namespace RVis.Model.Test
{
  [TestClass()]
  public class UnitTestLogNormalDistribution
  {
#if !IS_PIPELINES_BUILD
    [TestMethod()]
    public async Task TestLogNormalCumulativeDistributionAtBounds()
    {
      // arrange
      var subject = new LogNormalDistribution(0d, 1d, Log(0.3), Log(0.7));
      var expectedLowerP = (await GetNumDataAsync("plnorm(0.3, 0.0, 1.0)"))[0].Data[0];
      var expectedUpperP = (await GetNumDataAsync("plnorm(0.7, 0.0, 1.0)"))[0].Data[0];

      // act
      var (actualLowerP, actualUpperP) = subject.CumulativeDistributionAtBounds;

      // assert
      Assert.AreEqual(expectedLowerP, actualLowerP, Base.Constant.TOLERANCE);
      Assert.AreEqual(expectedUpperP, actualUpperP, Base.Constant.TOLERANCE);
    }
#endif

    [TestMethod()]
    public void TestLogNormalWithBoundsFillSamples()
    {
      // arrange
      var subject = new LogNormalDistribution(0d, 1d, Log(0.3), Log(0.7));
      var samples = Range(0, 42).Map(_ => NaN).ToArray();

      // act
      subject.FillSamples(samples);

      // assert
      Assert.IsTrue(samples.All(d => d > 0.3 && d < 0.7));
    }

    [TestMethod()]
    public void TestLogNormalWithBoundsGetSample()
    {
      // arrange
      var subject = new LogNormalDistribution(0d, 1d, Log(0.3), Log(0.7));

      // act
      var sample = subject.GetSample();

      // assert
      Assert.IsTrue(sample > 0.3 && sample < 0.7);
    }

#if !IS_PIPELINES_BUILD
    [TestMethod()]
    public async Task TestLogNormalGetDensities()
    {
      // arrange
      var subject = new LogNormalDistribution(0d, 1d);
      var expected = (await GetNumDataAsync(
        "sapply(seq(qlnorm(0.3, 0.0, 1.0), qlnorm(0.7, 0.0, 1.0), length.out = 5), function(cd){dlnorm(cd, 0.0, 1.0)})"
        ))[0].Data.ToArr();

      // act
      var (_, actual) = subject.GetDensities(0.3, 0.7, 5);

      // assert
      Assert.IsTrue(actual.Count == 5);
      expected.Iter((i, d) => Assert.AreEqual(d, actual[i], Base.Constant.TOLERANCE));
    }
#endif

    [TestMethod()]
    public void TestLogNormalRoundTrip()
    {
      // arrange
      var expected = new LogNormalDistribution(0d, 1d, 0.3, 0.7);

      // act
      var serialized = expected.ToString();
      var deserialized = Distribution.DeserializeDistribution(serialized);
      var distribution = deserialized.IfNone(() => { Assert.Fail(); return default!; });

      // assert
      var actual = Base.Check.RequireInstanceOf<LogNormalDistribution>(distribution);
      Assert.AreEqual(expected, actual);
    }
  }
}