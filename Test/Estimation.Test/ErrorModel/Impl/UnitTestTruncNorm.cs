using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using static Estimation.Test.ErrorModelImpl;
using static RVis.Base.Constant;

namespace Estimation.Test
{
  [TestClass]
  public class UnitTestTruncNorm
  {
#if !IS_PIPELINES_BUILD
    [TestMethod]
    public async Task TestDTruncNorm()
    {
      // arrange
      var lower = -0.5;
      var upper = 3d;
      var mean = 1d;
      var standardDeviation = 1d;

      var x1 = mean;
      var x2 = lower - 1d;
      var x3 = upper + 1d;

      var expected1 = (await GetNumDataAsync($"truncnorm::dtruncnorm({x1},{lower},{upper},{mean},{standardDeviation})"))[0].Data[0];
      var expected2 = (await GetNumDataAsync($"truncnorm::dtruncnorm({x2},{lower},{upper},{mean},{standardDeviation})"))[0].Data[0];
      var expected3 = (await GetNumDataAsync($"truncnorm::dtruncnorm({x3},{lower},{upper},{mean},{standardDeviation})"))[0].Data[0];

      // act
      var actual1 = TruncNorm.DTruncNorm(x1, lower, upper, mean, standardDeviation);
      var actual2 = TruncNorm.DTruncNorm(x2, lower, upper, mean, standardDeviation);
      var actual3 = TruncNorm.DTruncNorm(x3, lower, upper, mean, standardDeviation);

      // assert
      Assert.AreEqual(expected1, actual1, TOLERANCE);
      Assert.AreEqual(expected2, actual2, TOLERANCE);
      Assert.AreEqual(expected3, actual3, TOLERANCE);
    }
#endif
  }
}
