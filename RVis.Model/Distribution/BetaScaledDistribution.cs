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
  public struct BetaScaledDistribution : IDistribution<BetaScaled>, IEquatable<BetaScaledDistribution>
  {
    public readonly static BetaScaledDistribution Default = new BetaScaledDistribution(NaN, NaN, NaN, NaN);

    public BetaScaledDistribution(
      double alpha,
      double beta,
      double location,
      double scale,
      double lower = NegativeInfinity,
      double upper = PositiveInfinity
      )
    {
      RequireTrue(lower < upper, "Expecting lower < upper");

      Alpha = alpha;
      Beta = beta;
      Location = location;
      Scale = scale;
      Lower = lower;
      Upper = upper;
    }

    public double Alpha { get; }
    public double Beta { get; }
    public double Location { get; }
    public double Scale { get; }
    public double Lower { get; }
    public double Upper { get; }

    public BetaScaled? Implementation => IsConfigured
      ? new BetaScaled(Alpha, Beta, Location, Scale, Generator)
      : default;

    public DistributionType DistributionType => DistributionType.BetaScaled;

    public bool CanTruncate => true;

    public bool IsTruncated => !IsNegativeInfinity(Lower) || !IsPositiveInfinity(Upper);

    IDistribution IDistribution.WithLowerUpper(double lower, double upper) =>
      WithLowerUpper(lower, upper);

    public BetaScaledDistribution WithLowerUpper(double lower, double upper) =>
      new BetaScaledDistribution(Alpha, Beta, Location, Scale, lower, upper);

    public bool IsConfigured => !(IsNaN(Alpha) || IsNaN(Beta) || IsNaN(Location) || IsNaN(Scale));

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

      if (!IsTruncated) return BetaScaled.Sample(Generator, Alpha, Beta, Location, Scale);

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
      return BetaScaled.InvCDF(Alpha, Beta, Location, Scale, p);
    }

    public (string? FunctionName, Arr<(string ArgName, double ArgValue)> FunctionParameters) RQuantileSignature =>
      default;

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
      var location = Location;
      var scale = Scale;

      var cdLower = BetaScaled.InvCDF(alpha, beta, location, scale, lowerP);
      var cdUpper = BetaScaled.InvCDF(alpha, beta, location, scale, upperP);

      var division = (cdUpper - cdLower) / (nDensities - 1);
      var values = Range(0, nDensities).Map(i => cdLower + i * division).ToArr();
      var densities = values.Map(v => BetaScaled.PDF(alpha, beta, location, scale, v));

      return (values, densities);
    }

    public double GetDensity(double value)
    {
      RequireTrue(IsConfigured);

      return BetaScaled.PDF(Alpha, Beta, Location, Scale, value);
    }

    public override string ToString() =>
      SerializeDistribution(
        DistributionType,
        IsTruncated
          ? Array<object>(Alpha, Beta, Location, Scale, Lower, Upper)
          : Array<object>(Alpha, Beta, Location, Scale)
        );

    public string ToString(string variableName)
    {
      var alpha = IsNaN(Alpha) ? "?" : Alpha.ToString("G4", CurrentCulture);
      var beta = IsNaN(Beta) ? "?" : Beta.ToString("G4", CurrentCulture);
      var location = IsNaN(Location) ? "?" : Location.ToString("G4", CurrentCulture);
      var scale = IsNaN(Scale) ? "?" : Scale.ToString("G4", CurrentCulture);

      var distribution = $"{variableName} ~ Beta(α = {alpha}, β = {beta}, µ = {location}, σ = {scale})";

      if (!IsTruncated) return distribution;

      var interval = "[" +
        (IsNegativeInfinity(Lower) ? "-∞" : Lower.ToString("G4", InvariantCulture)) +
        ", " +
        (IsPositiveInfinity(Upper) ? "+∞" : Upper.ToString("G4", InvariantCulture)) +
        "]";

      return $"{distribution} {interval}";
    }

    public bool Equals(BetaScaledDistribution rhs) =>
      Alpha.Equals(rhs.Alpha) &&
      Beta.Equals(rhs.Beta) &&
      Location.Equals(rhs.Location) &&
      Scale.Equals(rhs.Scale) &&
      Lower.Equals(rhs.Lower) &&
      Upper.Equals(rhs.Upper);

    public override bool Equals(object? obj)
    {
      if (obj is BetaScaledDistribution rhs)
      {
        return Equals(rhs);
      }

      return false;
    }

    public override int GetHashCode() => 
      HashCode.Combine(DistributionType, Alpha, Beta, Location, Scale, Lower, Upper);

    public static bool operator ==(BetaScaledDistribution left, BetaScaledDistribution right)
    {
      return left.Equals(right);
    }

    public static bool operator !=(BetaScaledDistribution left, BetaScaledDistribution right)
    {
      return !(left == right);
    }

    internal static Option<IDistribution> Parse(Arr<string> parts)
    {
      if (parts.Count < 4) return None;
      if (!TryParse(parts[0], NumberStyles.Any, InvariantCulture, out double alpha)) return None;
      if (!TryParse(parts[1], NumberStyles.Any, InvariantCulture, out double beta)) return None;
      if (!TryParse(parts[2], NumberStyles.Any, InvariantCulture, out double location)) return None;
      if (!TryParse(parts[3], NumberStyles.Any, InvariantCulture, out double scale)) return None;
      if (parts.Count != 6) return new BetaScaledDistribution(alpha, beta, location, scale);
      if (!TryParse(parts[4], NumberStyles.Any, InvariantCulture, out double lower)) return None;
      if (!TryParse(parts[5], NumberStyles.Any, InvariantCulture, out double upper)) return None;
      return new BetaScaledDistribution(alpha, beta, location, scale, lower, upper);
    }
  }
}
