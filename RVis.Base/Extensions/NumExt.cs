using static RVis.Base.Constant;
using static System.Double;
using static System.Math;

namespace RVis.Base.Extensions
{
  public static class NumExt
  {
    public const int NOT_FOUND = -1;
    private const double POSITIVE = 1.0;
    private const double ZERO = 0.0;
    private const double NEGATIVE = -1.0;

    public static double? ToNullable(this double d) =>
      IsNaN(d) ? default(double?) : d;

    public static double FromNullable(this double? d) =>
      d ?? NaN;

    public static double GetSignum(this double d) =>
      d == 0.0 ? ZERO : d < 0.0 ? NEGATIVE : POSITIVE;

    public static bool IsFound(this int i)
      => i != NOT_FOUND;

    public static bool IsntFound(this int i)
      => !i.IsFound();

    public static double ToSigFigs(this double d, int digits)
    {
      if (d == 0.0) return d;
      var scale = Pow(10, Floor(Log10(Abs(d))) + 1);
      return scale * Round(d / scale, digits);
    }

    public static bool IsEqualTo(this double lhs, double rhs) =>
      Abs(lhs - rhs) < TOLERANCE;

    public static bool IsInClosedInterval(this double d, double leftBound, double rightBound) =>
      d >= leftBound && d <= rightBound;

    public static double GetPreviousOrderOfMagnitude(this double d)
    {
      var signum = d.GetSignum();

      if (signum == ZERO) return -1.0;

      var oom = GetOrderOfMagnitude(d, signum == POSITIVE);

      return Pow(10.0, oom) * signum;
    }

    public static double GetNextOrderOfMagnitude(this double d)
    {
      var signum = d.GetSignum();

      if (signum == ZERO) return 1.0;

      var oom = GetOrderOfMagnitude(d, signum == NEGATIVE);

      return Pow(10.0, oom) * signum;
    }

    private static double GetOrderOfMagnitude(double from, bool goDownwards)
    {
      var absd = Abs(from);
      var log10d = Log10(absd);

      double oom;

      if (goDownwards)
      {
        oom = Floor(log10d);
        if (oom == log10d) --oom;
      }
      else
      {
        oom = Ceiling(log10d);
        if (oom == log10d) ++oom;
      }

      return oom;
    }
  }
}
