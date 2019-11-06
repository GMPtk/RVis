using static MathNet.Numerics.Distributions.Normal;
using static RVis.Base.Check;
using static System.Double;
using static System.Math;

namespace Estimation
{
  internal static class TruncNorm
  {
    internal static double DTruncNorm(double x, double lower, double upper, double mean, double standardDeviation)
    {
      RequireTrue(lower < upper);
      RequireTrue(standardDeviation > 0d);

      // https://github.com/olafmersmann/truncnorm/blob/eea186ebf0a681051dcfe5ec2247860d33fd21e7/src/truncnorm.c#L151

      if (IsNegativeInfinity(lower) && IsPositiveInfinity(upper))
      {
        return PDF(mean, standardDeviation, x);
      }

      if (x < lower || x > upper) return 0d;

      var pLower = CDF(mean, standardDeviation, lower);
      var pUpper = CDF(mean, standardDeviation, upper);

      var logUnnorm = Log(standardDeviation * (pUpper - pLower));

      if (IsInfinity(logUnnorm)) return 1d / (upper - lower);

      var logNorm = PDFLn(0d, 1d, (x - mean) / standardDeviation);

      return Exp(logNorm - logUnnorm);
    }
  }
}
