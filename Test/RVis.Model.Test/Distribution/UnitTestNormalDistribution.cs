using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;
using static LanguageExt.Prelude;
using static RVis.Model.Test.DistributionImpl;
using static System.Double;

namespace RVis.Model.Test
{
  [TestClass()]
  public class UnitTestNormalDistribution
  {
#if !IS_PIPELINES_BUILD
    [TestMethod()]
    public async Task TestNormalCumulativeDistributionAtBounds()
    {
      // arrange
      var subject = new NormalDistribution(0.5, 1d, 0.3, 0.7);
      var expectedLowerP = (await GetNumDataAsync("pnorm(0.3, 0.5, 1.0)"))[0].Data[0];
      var expectedUpperP = (await GetNumDataAsync("pnorm(0.7, 0.5, 1.0)"))[0].Data[0];

      // act
      var (actualLowerP, actualUpperP) = subject.CumulativeDistributionAtBounds;

      // assert
      Assert.AreEqual(expectedLowerP, actualLowerP, Base.Constant.TOLERANCE);
      Assert.AreEqual(expectedUpperP, actualUpperP, Base.Constant.TOLERANCE);
    }
#endif

    [TestMethod()]
    public void TestNormalWithBoundsFillSamples()
    {
      // arrange
      var subject = new NormalDistribution(0.5, 1d, 0.3, 0.7);
      var samples = Range(0, 42).Map(_ => NaN).ToArray();

      // act
      subject.FillSamples(samples);

      // assert
      Assert.IsTrue(samples.All(d => d > 0.3 && d < 0.7));
    }

    [TestMethod()]
    public void TestNormalWithBoundsGetSample()
    {
      // arrange
      var subject = new NormalDistribution(0.5, 1d, 0.3, 0.7);

      // act
      var sample = subject.GetSample();

      // assert
      Assert.IsTrue(sample > 0.3 && sample < 0.7);
    }

#if !IS_PIPELINES_BUILD
    [TestMethod()]
    public async Task TestNormalGetDensities()
    {
      // arrange
      var subject = new NormalDistribution(0.5, 1d);
      var expected = (await GetNumDataAsync(
        "sapply(seq(qnorm(0.3, 0.5, 1.0), qnorm(0.7, 0.5, 1.0), length.out = 5), function(cd){dnorm(cd, 0.5, 1.0)})"
        ))[0].Data.ToArr();

      // act
      var (_, actual) = subject.GetDensities(0.3, 0.7, 5);

      // assert
      Assert.IsTrue(actual.Count == 5);
      expected.Iter((i, d) => Assert.AreEqual(d, actual[i], Base.Constant.TOLERANCE));
    }
#endif

    [TestMethod()]
    public void TestNormalRoundTrip()
    {
      // arrange
      var expected = new NormalDistribution(0.5, 1d, 0.3, 0.7);

      // act
      var serialized = expected.ToString();
      var deserialized = Distribution.DeserializeDistribution(serialized);
      var distribution = deserialized.IfNone(() => { Assert.Fail(); return default!; });

      // assert
      var actual = Base.Check.RequireInstanceOf<NormalDistribution>(distribution);
      Assert.AreEqual(expected, actual);
    }
  }
}