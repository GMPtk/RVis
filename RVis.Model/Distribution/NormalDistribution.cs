using LanguageExt;
using MathNet.Numerics.Distributions;
using RVis.Base.Extensions;
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
  public struct NormalDistribution : IDistribution<Normal>, IEquatable<NormalDistribution>
  {
    public readonly static NormalDistribution Default = new NormalDistribution(NaN, NaN);

    public static NormalDistribution CreateDefault(double mu)
    {
      var sigma = mu == 0d ? 1d : (Abs(mu) / 5d).ToSigFigs(1);
      var lower = (mu - 2d * sigma).ToSigFigs(2);
      var upper = (mu + 2d * sigma).ToSigFigs(2);
      return new NormalDistribution(mu, sigma, lower, upper);
    }

    public NormalDistribution(double mu, double sigma, double lower = NegativeInfinity, double upper = PositiveInfinity)
    {
      RequireTrue(IsNaN(sigma) || sigma > 0, "Expecting σ > 0");
      RequireTrue(lower < upper, "Expecting lower < upper");
      RequireTrue(IsNaN(mu) || mu >= lower, "µ out of bounds");
      RequireTrue(IsNaN(mu) || mu <= upper, "µ out of bounds");

      Mu = mu;
      Sigma = sigma;
      Lower = lower;
      Upper = upper;
    }

    public double Mu { get; }
    public double Sigma { get; }
    public double Lower { get; }
    public double Upper { get; }

    public Normal? Implementation => IsConfigured
      ? new Normal(Mu, Sigma, Generator)
      : default;

    public DistributionType DistributionType => DistributionType.Normal;

    public bool CanTruncate => true;

    public bool IsTruncated => !IsNegativeInfinity(Lower) || !IsPositiveInfinity(Upper);

    IDistribution IDistribution.WithLowerUpper(double lower, double upper) =>
      WithLowerUpper(lower, upper);

    public NormalDistribution WithLowerUpper(double lower, double upper) =>
      new NormalDistribution(Max(lower, Min(Mu, upper)), Sigma, lower, upper);

    public bool IsConfigured => !IsNaN(Mu) && !IsNaN(Sigma);

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

      if (!IsTruncated) return Normal.Sample(Generator, Mu, Sigma);

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

      value += Normal.Sample(Generator, 0, 1) * step;

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
      return Normal.InvCDF(Mu, Sigma, p);
    }

    public (string? FunctionName, Arr<(string ArgName, double ArgValue)> FunctionParameters) RQuantileSignature =>
      ("qnorm", Array(("mean", Mu), ("sd", Sigma)));

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

      var mu = Mu;
      var sigma = Sigma;

      var cdLower = Normal.InvCDF(mu, sigma, lowerP);
      var cdUpper = Normal.InvCDF(mu, sigma, upperP);

      var division = (cdUpper - cdLower) / (nDensities - 1);
      var values = Range(0, nDensities).Map(i => cdLower + i * division).ToArr();
      var densities = values.Map(v => Normal.PDF(mu, sigma, v));

      return (values, densities);
    }

    public double GetDensity(double value)
    {
      RequireTrue(IsConfigured);

      return Normal.PDF(Mu, Sigma, value);
    }

    public override string ToString() =>
      SerializeDistribution(
        DistributionType,
        IsTruncated
          ? Array<object>(Mu, Sigma, Lower, Upper)
          : Array<object>(Mu, Sigma)
        );

    public string ToString(string variableName)
    {
      var mean = IsNaN(Mu) ? "?" : Mu.ToString("G4", CurrentCulture);
      var variance = IsNaN(Sigma) ? "?" : (Sigma * Sigma).ToString("G4", CurrentCulture);
      var distribution = $"{variableName} ~ N(µ = {mean}, σ² = {variance})";

      if (!IsTruncated) return distribution;

      var interval = "[" +
        (IsNegativeInfinity(Lower) ? "-∞" : Lower.ToString("G4", InvariantCulture)) +
        ", " +
        (IsPositiveInfinity(Upper) ? "+∞" : Upper.ToString("G4", InvariantCulture)) +
        "]";

      return $"{distribution} {interval}";
    }

    public bool Equals(NormalDistribution rhs) =>
      Mu.Equals(rhs.Mu) && Sigma.Equals(rhs.Sigma) && Lower.Equals(rhs.Lower) && Upper.Equals(rhs.Upper);

    public override bool Equals(object? obj)
    {
      if (obj is NormalDistribution rhs)
      {
        return Equals(rhs);
      }

      return false;
    }

    public override int GetHashCode() => 
      HashCode.Combine(DistributionType, Mu, Sigma, Lower, Upper);

    public static bool operator ==(NormalDistribution left, NormalDistribution right)
    {
      return left.Equals(right);
    }

    public static bool operator !=(NormalDistribution left, NormalDistribution right)
    {
      return !(left == right);
    }

    internal static Option<IDistribution> Parse(Arr<string> parts)
    {
      if (parts.Count < 2) return None;
      if (!TryParse(parts[0], NumberStyles.Any, InvariantCulture, out double mu)) return None;
      if (!TryParse(parts[1], NumberStyles.Any, InvariantCulture, out double sigma)) return None;
      if (parts.Count != 4) return new NormalDistribution(mu, sigma);
      if (!TryParse(parts[2], NumberStyles.Any, InvariantCulture, out double lower)) return None;
      if (!TryParse(parts[3], NumberStyles.Any, InvariantCulture, out double upper)) return None;
      return new NormalDistribution(mu, sigma, lower, upper);
    }
  }
}
