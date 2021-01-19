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
  public struct LogNormalDistribution : IDistribution<LogNormal>, IEquatable<LogNormalDistribution>
  {
    public readonly static LogNormalDistribution Default = new LogNormalDistribution(NaN, NaN);

    public LogNormalDistribution(double mu, double sigma, double lower = NegativeInfinity, double upper = PositiveInfinity)
    {
      RequireTrue(IsNaN(sigma) || sigma > 0, "Expecting σ > 0");
      RequireTrue(lower < upper, "Expecting lower < upper");

      Mu = mu;
      Sigma = sigma;
      Lower = lower;
      Upper = upper;
    }

    public double Mu { get; }
    public double Sigma { get; }
    public double Lower { get; }
    public double Upper { get; }

    public LogNormal? Implementation => IsConfigured
      ? new LogNormal(Mu, Sigma, Generator)
      : default;

    public DistributionType DistributionType => DistributionType.LogNormal;

    public bool CanTruncate => true;

    public bool IsTruncated => !IsNegativeInfinity(Lower) || !IsPositiveInfinity(Upper);

    IDistribution IDistribution.WithLowerUpper(double lower, double upper) =>
      WithLowerUpper(lower, upper);

    public LogNormalDistribution WithLowerUpper(double lower, double upper) =>
      lower > 0d && upper > 0d 
      ? new LogNormalDistribution(Mu, Sigma, Log(lower), Log(upper)) 
      : this;

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
        var implementation = Implementation!;
        var lowerP = implementation.CumulativeDistribution(Exp(Lower));
        var upperP = implementation.CumulativeDistribution(Exp(Upper));
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

      var lowerP = implementation.CumulativeDistribution(Exp(Lower));
      var upperP = implementation.CumulativeDistribution(Exp(Upper));
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

      if (!IsTruncated) return LogNormal.Sample(Generator, Mu, Sigma);

      var implementation = Implementation!;
      var lowerP = implementation.CumulativeDistribution(Exp(Lower));
      var upperP = implementation.CumulativeDistribution(Exp(Upper));
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
        value = Max(Exp(Lower), Min(value, Exp(Upper)));
      }

      return value;
    }

    public double ApplyBias(double step, double bias) =>
      step * bias;

    public double InverseCumulativeDistribution(double p)
    {
      RequireTrue(IsConfigured);
      return LogNormal.InvCDF(Mu, Sigma, p);
    }

    public (string? FunctionName, Arr<(string ArgName, double ArgValue)> FunctionParameters) RQuantileSignature =>
      ("qlnorm", Array(("mean", Mu), ("sd", Sigma)));

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

      var cdLower = LogNormal.InvCDF(mu, sigma, lowerP);
      var cdUpper = LogNormal.InvCDF(mu, sigma, upperP);

      var division = (cdUpper - cdLower) / (nDensities - 1);
      var values = Range(0, nDensities).Map(i => cdLower + i * division).ToArr();
      var densities = values.Map(v => LogNormal.PDF(mu, sigma, v));

      return (values, densities);
    }

    public double GetDensity(double value)
    {
      RequireTrue(IsConfigured);

      return LogNormal.PDF(Mu, Sigma, value);
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

      var distribution = $"ln({variableName}) ~ N(µ = {mean}, σ² = {variance})";

      if (!IsTruncated) return distribution;

      var interval = "[" +
        (IsNegativeInfinity(Lower) ? "-∞" : Lower.ToString("G4", InvariantCulture)) +
        ", " +
        (IsPositiveInfinity(Upper) ? "+∞" : Upper.ToString("G4", InvariantCulture)) +
        "]";

      return $"{distribution} {interval}";
    }

    public bool Equals(LogNormalDistribution rhs) =>
      Mu.Equals(rhs.Mu) && Sigma.Equals(rhs.Sigma) && Lower.Equals(rhs.Lower) && Upper.Equals(rhs.Upper);

    public override bool Equals(object? obj)
    {
      if (obj is LogNormalDistribution rhs)
      {
        return Equals(rhs);
      }

      return false;
    }

    public override int GetHashCode() => 
      HashCode.Combine(DistributionType, Mu, Sigma, Lower, Upper);

    public static bool operator ==(LogNormalDistribution left, LogNormalDistribution right)
    {
      return left.Equals(right);
    }

    public static bool operator !=(LogNormalDistribution left, LogNormalDistribution right)
    {
      return !(left == right);
    }

    internal static Option<IDistribution> Parse(Arr<string> parts)
    {
      if (parts.Count < 2) return None;
      if (!TryParse(parts[0], NumberStyles.Any, InvariantCulture, out double mu)) return None;
      if (!TryParse(parts[1], NumberStyles.Any, InvariantCulture, out double sigma)) return None;
      if (parts.Count != 4) return new LogNormalDistribution(mu, sigma);
      if (!TryParse(parts[2], NumberStyles.Any, InvariantCulture, out double lower)) return None;
      if (!TryParse(parts[3], NumberStyles.Any, InvariantCulture, out double upper)) return None;
      return new LogNormalDistribution(mu, sigma, lower, upper);
    }
  }
}
