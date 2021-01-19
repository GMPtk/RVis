using LanguageExt;
using MathNet.Numerics.Distributions;
using System;
using System.Globalization;
using static LanguageExt.Prelude;
using static RVis.Base.Check;
using static RVis.Model.Distribution;
using static RVis.Model.RandomNumberGenerator;
using static System.Double;
using static System.Globalization.CultureInfo;
using static System.Math;

namespace RVis.Model
{
  public struct GammaDistribution : IDistribution<Gamma>, IEquatable<GammaDistribution>
  {
    public readonly static GammaDistribution Default = new GammaDistribution(NaN, NaN);

    public GammaDistribution(double alpha, double beta, double lower = NegativeInfinity, double upper = PositiveInfinity)
    {
      RequireTrue(lower < upper, "Expecting lower < upper");

      Alpha = alpha;
      Beta = beta;
      Lower = lower;
      Upper = upper;
    }

    public double Alpha { get; }
    public double Beta { get; }
    public double Lower { get; }
    public double Upper { get; }

    public Gamma? Implementation => IsConfigured
      ? new Gamma(Alpha, Beta, Generator)
      : default;

    public DistributionType DistributionType => DistributionType.Gamma;

    public bool CanTruncate => true;

    public bool IsTruncated => !IsNegativeInfinity(Lower) || !IsPositiveInfinity(Upper);

    IDistribution IDistribution.WithLowerUpper(double lower, double upper) =>
      WithLowerUpper(lower, upper);

    public GammaDistribution WithLowerUpper(double lower, double upper) =>
      new GammaDistribution(Alpha, Beta, lower, upper);

    public bool IsConfigured => !IsNaN(Alpha) && !IsNaN(Beta);

    public double Mean
    {
      get
      {
        RequireTrue(IsConfigured);
        return Implementation!.Mean;
      }
    }

    public double Variance
    {
      get
      {
        RequireTrue(IsConfigured);
        return Implementation!.Variance;
      }
    }

    public (double LowerP, double UpperP) CumulativeDistributionAtBounds
    {
      get
      {
        RequireTrue(IsConfigured);

        var implementation = Implementation!;
        var lowerP = implementation.CumulativeDistribution(Lower);
        var upperP = implementation.CumulativeDistribution(Upper);
        return (lowerP, upperP);
      }
    }

    public void FillSamples(double[] samples)
    {
      RequireTrue(IsConfigured);
      RequireNotNull(samples);

      var implementation = Implementation!;

      if (!IsTruncated)
      {
        implementation.Samples(samples);
        return;
      }

      var lowerP = implementation.CumulativeDistribution(Lower);
      var upperP = implementation.CumulativeDistribution(Upper);
      var uniform = new ContinuousUniform(lowerP, upperP, Generator);
      uniform.Samples(samples);

      for (var i = 0; i < samples.Length; ++i)
      {
        samples[i] = implementation.InverseCumulativeDistribution(samples[i]);
      }
    }

    public double GetSample()
    {
      RequireTrue(IsConfigured);

      if (!IsTruncated) return Gamma.Sample(Generator, Alpha, Beta);

      var implementation = Implementation!;
      var lowerP = implementation.CumulativeDistribution(Lower);
      var upperP = implementation.CumulativeDistribution(Upper);
      var sample = ContinuousUniform.Sample(Generator, lowerP, upperP);
      return implementation.InverseCumulativeDistribution(sample);
    }

    public double GetProposal(double value, double step)
    {
      RequireTrue(!IsNaN(step) || IsConfigured);

      if (IsNaN(value)) value = Mean;
      if (IsNaN(step)) step = Implementation!.Variance;

      var sample = GetSample();
      value += (sample - value) * step;

      if (IsTruncated)
      {
        value = Max(Lower, Min(value, Upper));
      }

      return value;
    }

    public double ApplyBias(double step, double bias) =>
      step * bias;

    public double InverseCumulativeDistribution(double p)
    {
      RequireTrue(IsConfigured);
      return Gamma.InvCDF(Alpha, Beta, p);
    }

    public (string? FunctionName, Arr<(string ArgName, double ArgValue)> FunctionParameters) RQuantileSignature =>
      ("qgamma", Array(("shape", Alpha), ("rate", Beta)));

    public (string FunctionName, Arr<(string ArgName, double ArgValue)> FunctionParameters) RInverseTransformSamplingSignature
    {
      get
      {
        var (lowerP, upperP) = CumulativeDistributionAtBounds;
        return ("qunif", Array(("min", lowerP), ("max", upperP))); ;
      }
    }

    public (Arr<double> Values, Arr<double> Densities) GetDensities(double lowerP, double upperP, int nDensities)
    {
      RequireTrue(IsConfigured);
      RequireTrue(nDensities > 1);

      var alpha = Alpha;
      var beta = Beta;

      var cdLower = Gamma.InvCDF(alpha, beta, lowerP);
      var cdUpper = Gamma.InvCDF(alpha, beta, upperP);

      var division = (cdUpper - cdLower) / (nDensities - 1);
      var values = Range(0, nDensities).Map(i => cdLower + i * division).ToArr();
      var densities = values.Map(v => Gamma.PDF(alpha, beta, v));

      return (values, densities);
    }

    public double GetDensity(double value)
    {
      RequireTrue(IsConfigured);

      return Gamma.PDF(Alpha, Beta, value);
    }

    public override string ToString() =>
      SerializeDistribution(
        DistributionType,
        IsTruncated
          ? Array<object>(Alpha, Beta, Lower, Upper)
          : Array<object>(Alpha, Beta)
        );

    public string ToString(string variableName)
    {
      var alpha = IsNaN(Alpha) ? "?" : Alpha.ToString("G4", CurrentCulture);
      var beta = IsNaN(Beta) ? "?" : Beta.ToString("G4", CurrentCulture);

      var distribution = $"{variableName} ~ Γ(α = {alpha}, β = {beta})";

      if (!IsTruncated) return distribution;

      var interval = "[" +
        (IsNegativeInfinity(Lower) ? "-∞" : Lower.ToString("G4", InvariantCulture)) +
        ", " +
        (IsPositiveInfinity(Upper) ? "+∞" : Upper.ToString("G4", InvariantCulture)) +
        "]";

      return $"{distribution} {interval}";
    }

    public bool Equals(GammaDistribution rhs) =>
      Alpha.Equals(rhs.Alpha) && Beta.Equals(rhs.Beta) && Lower.Equals(rhs.Lower) && Upper.Equals(rhs.Upper);

    public override bool Equals(object? obj)
    {
      if (obj is GammaDistribution rhs)
      {
        return Equals(rhs);
      }

      return false;
    }

    public override int GetHashCode() => 
      HashCode.Combine(DistributionType, Alpha, Beta, Lower, Upper);

    public static bool operator ==(GammaDistribution left, GammaDistribution right)
    {
      return left.Equals(right);
    }

    public static bool operator !=(GammaDistribution left, GammaDistribution right)
    {
      return !(left == right);
    }

    internal static Option<IDistribution> Parse(Arr<string> parts)
    {
      if (parts.Count < 2) return None;
      if (!TryParse(parts[0], NumberStyles.Any, InvariantCulture, out double alpha)) return None;
      if (!TryParse(parts[1], NumberStyles.Any, InvariantCulture, out double beta)) return None;
      if (parts.Count != 4) return new GammaDistribution(alpha, beta);
      if (!TryParse(parts[2], NumberStyles.Any, InvariantCulture, out double lower)) return None;
      if (!TryParse(parts[3], NumberStyles.Any, InvariantCulture, out double upper)) return None;
      return new GammaDistribution(alpha, beta, lower, upper);
    }
  }
}
