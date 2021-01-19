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

namespace RVis.Model
{
  public struct InverseGammaDistribution : IDistribution<InverseGamma>, IEquatable<InverseGammaDistribution>
  {
    public readonly static InverseGammaDistribution Default = new InverseGammaDistribution(NaN, NaN);

    public InverseGammaDistribution(double alpha, double beta)
    {
      Alpha = alpha;
      Beta = beta;
    }

    public double Alpha { get; }
    public double Beta { get; }

    public InverseGamma? Implementation => IsConfigured
      ? new InverseGamma(Alpha, Beta, Generator)
      : default;

    public DistributionType DistributionType => DistributionType.InverseGamma;

    public bool CanTruncate => false;

    public bool IsTruncated => false;

    public IDistribution WithLowerUpper(double lower, double upper) =>
      this;

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
      => throw new InvalidOperationException("Operation not supported");

    public void FillSamples(double[] samples)
    {
      RequireTrue(IsConfigured);
      RequireNotNull(samples);

      var implementation = Implementation!;
      implementation.Samples(samples);
    }

    public double GetSample()
    {
      RequireTrue(IsConfigured);
      return InverseGamma.Sample(Generator, Alpha, Beta);
    }

    public double GetProposal(double value, double step)
    {
      RequireTrue(!IsNaN(step) || IsConfigured);

      if (IsNaN(value)) value = Mean;
      if (IsNaN(step)) step = Implementation!.Variance;

      var sample = GetSample();
      value += (sample - value) * step;

      return value;
    }

    public double ApplyBias(double step, double bias) =>
      step * bias;

    public double InverseCumulativeDistribution(double p) =>
      throw new InvalidOperationException("Not a continuous random variable");

    public (string? FunctionName, Arr<(string ArgName, double ArgValue)> FunctionParameters) RQuantileSignature =>
      (default, default);

    public (string FunctionName, Arr<(string ArgName, double ArgValue)> FunctionParameters) RInverseTransformSamplingSignature =>
      default;

    public (Arr<double> Values, Arr<double> Densities) GetDensities(double lowerP, double upperP, int nDensities) =>
      throw new InvalidOperationException("Not a continuous random variable");

    public double GetDensity(double value) =>
      throw new InvalidOperationException("Not a continuous random variable");

    public override string ToString() =>
      SerializeDistribution(DistributionType, Array<object>(Alpha, Beta));

    public string ToString(string variableName)
    {
      var alpha = IsNaN(Alpha) ? "?" : Alpha.ToString("G4", CurrentCulture);
      var beta = IsNaN(Beta) ? "?" : Beta.ToString("G4", CurrentCulture);

      return $"{variableName} ~ Inv-Gamma(α = {alpha}, β = {beta})";
    }

    public bool Equals(InverseGammaDistribution rhs) =>
      Alpha.Equals(rhs.Alpha) && Beta.Equals(rhs.Beta);

    public override bool Equals(object? obj)
    {
      if (obj is InverseGammaDistribution rhs)
      {
        return Equals(rhs);
      }

      return false;
    }

    public override int GetHashCode() => 
      HashCode.Combine(DistributionType, Alpha, Beta);

    public static bool operator ==(InverseGammaDistribution left, InverseGammaDistribution right)
    {
      return left.Equals(right);
    }

    public static bool operator !=(InverseGammaDistribution left, InverseGammaDistribution right)
    {
      return !(left == right);
    }

    internal static Option<IDistribution> Parse(Arr<string> parts)
    {
      if (parts.Count != 2) return None;
      if (!TryParse(parts[0], NumberStyles.Any, InvariantCulture, out double alpha)) return None;
      if (!TryParse(parts[1], NumberStyles.Any, InvariantCulture, out double beta)) return None;
      return new InverseGammaDistribution(alpha, beta);
    }
  }
}
