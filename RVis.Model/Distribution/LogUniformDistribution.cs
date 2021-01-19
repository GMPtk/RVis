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
  public struct LogUniformDistribution : IDistribution<ContinuousUniform>, IEquatable<LogUniformDistribution>
  {
    public readonly static LogUniformDistribution Default = new LogUniformDistribution(NaN, NaN);

    public LogUniformDistribution(double lower, double upper)
    {
      RequireTrue(IsNaN(lower) || lower < upper, "Expecting lower < upper");

      Lower = lower;
      Upper = upper;
    }

    public double Lower { get; }
    public double Upper { get; }

    public ContinuousUniform? Implementation => IsConfigured
      ? new ContinuousUniform(Lower, Upper, Generator)
      : default;

    public DistributionType DistributionType => DistributionType.LogUniform;

    public bool CanTruncate => false;

    public bool IsTruncated => false;

    IDistribution IDistribution.WithLowerUpper(double lower, double upper) =>
      lower > 0d && upper > 0d 
      ? new LogUniformDistribution(Log(lower), Log(upper))
      : this;

    public bool IsConfigured => !IsNaN(Lower) && !IsNaN(Upper);

    public double Mean
    {
      get
      {
        RequireTrue(IsConfigured);
        return Exp(Implementation!.Mean);
      }
    }

    public double Variance
    {
      get
      {
        RequireTrue(IsConfigured);
        return Exp(Implementation!.Variance);
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
      implementation.Samples(samples);
      for (var i = 0; i < samples.Length; ++i) samples[i] = Exp(samples[i]);
    }

    public double GetSample()
    {
      RequireTrue(IsConfigured);
      return Exp(ContinuousUniform.Sample(Generator, Lower, Upper));
    }

    public double GetProposal(double value, double step)
    {
      RequireTrue(!IsNaN(step) || IsConfigured);

      if (IsNaN(value)) value = Mean;
      if (IsNaN(step)) step = Exp(Implementation!.Variance);

      var sample = GetSample();
      value += (sample - value) * step;

      return value;
    }

    public double ApplyBias(double step, double bias) =>
      step * bias;

    public double InverseCumulativeDistribution(double p)
    {
      RequireTrue(IsConfigured);
      return Exp(ContinuousUniform.InvCDF(Lower, Upper, p));
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

      var lower = Lower;
      var upper = Upper;

      var cdLower = ContinuousUniform.InvCDF(lower, upper, lowerP);
      var cdUpper = ContinuousUniform.InvCDF(lower, upper, upperP);

      var division = (cdUpper - cdLower) / (nDensities - 1);
      var values = Range(0, nDensities).Map(i => cdLower + i * division).ToArr();
      var densities = values.Map(x => ContinuousUniform.PDF(lower, upper, x));
      values = values.Map(Exp);

      return (values, densities);
    }

    public double GetDensity(double value)
    {
      RequireTrue(IsConfigured);

      return ContinuousUniform.PDF(Lower, Upper, Log(value));
    }

    public override string ToString() =>
      SerializeDistribution(DistributionType, Array<object>(Lower, Upper));

    public string ToString(string variableName)
    {
      var lower = IsNaN(Lower) ? "?" : Lower.ToString("G4", CurrentCulture);
      var upper = IsNaN(Upper) ? "?" : Upper.ToString("G4", CurrentCulture);

      return $"ln({variableName}) ~ U(a = {lower}, b = {upper})";
    }

    public bool Equals(LogUniformDistribution rhs) =>
      Lower.Equals(rhs.Lower) && Upper.Equals(rhs.Upper);

    public override bool Equals(object? obj)
    {
      if (obj is LogUniformDistribution rhs)
      {
        return Equals(rhs);
      }

      return false;
    }

    public override int GetHashCode() => 
      HashCode.Combine(DistributionType, Lower, Upper);

    public static bool operator ==(LogUniformDistribution left, LogUniformDistribution right)
    {
      return left.Equals(right);
    }

    public static bool operator !=(LogUniformDistribution left, LogUniformDistribution right)
    {
      return !(left == right);
    }

    internal static Option<IDistribution> Parse(Arr<string> parts)
    {
      if (parts.Count != 2) return None;
      if (!TryParse(parts[0], NumberStyles.Any, InvariantCulture, out double lower)) return None;
      if (!TryParse(parts[1], NumberStyles.Any, InvariantCulture, out double upper)) return None;
      return new LogUniformDistribution(lower, upper);
    }
  }
}
