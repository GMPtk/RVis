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
  public class UnitTestLogUniformDistribution
  {
#if !IS_PIPELINES_BUILD
    [TestMethod()]
    public async Task TestLogUniformCumulativeDistributionAtBounds()
    {
      // arrange
      var subject = new LogUniformDistribution(Log(0.1), Log(0.9));
      var expectedLowerP = (await GetNumDataAsync("punif(0.1, 0.1, 0.9)"))[0].Data[0];
      var expectedUpperP = (await GetNumDataAsync("punif(0.9, 0.1, 0.9)"))[0].Data[0];

      // act
      var (actualLowerP, actualUpperP) = subject.CumulativeDistributionAtBounds;

      // assert
      Assert.AreEqual(expectedLowerP, actualLowerP, Base.Constant.TOLERANCE);
      Assert.AreEqual(expectedUpperP, actualUpperP, Base.Constant.TOLERANCE);
    }
#endif

    [TestMethod()]
    public void TestLogUniformWithBoundsFillSamples()
    {
      // arrange
      var subject = new LogUniformDistribution(Log(0.1), Log(0.9));
      var samples = Range(0, 42).Map(_ => NaN).ToArray();

      // act
      subject.FillSamples(samples);

      // assert
      Assert.IsTrue(samples.All(d => d > 0.1 && d < 0.9));
    }

    [TestMethod()]
    public void TestLogUniformWithBoundsGetSample()
    {
      // arrange
      var subject = new LogUniformDistribution(Log(0.1), Log(0.9));

      // act
      var sample = subject.GetSample();

      // assert
      Assert.IsTrue(sample > 0.1 && sample < 0.9);
    }

#if !IS_PIPELINES_BUILD
    [TestMethod()]
    public async Task TestLogUniformGetDensities()
    {
      // arrange
      var subject = new LogUniformDistribution(Log(0.1), Log(0.9));
      var expected = (await GetNumDataAsync(
        "sapply(seq(qunif(0.3, log(0.1), log(0.9)), qunif(0.7, log(0.1), log(0.9)), length.out = 5), function(cd){dunif(cd, log(0.1), log(0.9))})"
        ))[0].Data.ToArr();

      // act
      var (_, actual) = subject.GetDensities(0.3, 0.7, 5);

      // assert
      Assert.IsTrue(actual.Count == 5);
      expected.Iter((i, d) => Assert.AreEqual(d, actual[i], Base.Constant.TOLERANCE));
    }
#endif

    [TestMethod()]
    public void TestLogUniformRoundTrip()
    {
      // arrange
      var expected = new LogUniformDistribution(Log(0.1), Log(0.9));

      // act
      var serialized = expected.ToString();
      var deserialized = Distribution.DeserializeDistribution(serialized);
      var distribution = deserialized.IfNone(() => { Assert.Fail(); return default!; });

      // assert
      var actual = Base.Check.RequireInstanceOf<LogUniformDistribution>(distribution);
      Assert.AreEqual(expected.Lower, actual.Lower, Base.Constant.TOLERANCE);
      Assert.AreEqual(expected.Upper, actual.Upper, Base.Constant.TOLERANCE);
    }
  }
}