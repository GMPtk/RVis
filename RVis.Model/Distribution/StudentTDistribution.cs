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
  public struct StudentTDistribution : IDistribution, IEquatable<StudentTDistribution>
  {
    public readonly static StudentTDistribution Default = new StudentTDistribution(NaN, NaN, NaN);

    public DistributionType DistributionType => DistributionType.StudentT;

    public StudentTDistribution(
      double mu,
      double sigma,
      double nu,
      double lower = NegativeInfinity,
      double upper = PositiveInfinity
      )
    {
      RequireTrue(lower < upper, "Expecting lower < upper");

      Mu = mu;
      Sigma = sigma;
      Nu = nu;
      Lower = lower;
      Upper = upper;
    }

    public double Mu { get; }
    public double Sigma { get; }
    public double Nu { get; }
    public double Lower { get; }
    public double Upper { get; }

    public StudentT? Implementation => IsConfigured
      ? new StudentT(Mu, Sigma, Nu, Generator)
      : default;

    public bool CanTruncate => true;

    public bool IsTruncated => !IsNegativeInfinity(Lower) || !IsPositiveInfinity(Upper);

    IDistribution IDistribution.WithLowerUpper(double lower, double upper) =>
      WithLowerUpper(lower, upper);

    public StudentTDistribution WithLowerUpper(double lower, double upper) =>
      new StudentTDistribution(Mu, Sigma, Nu, lower, upper);

    public bool IsConfigured => !(IsNaN(Mu) || IsNaN(Sigma) || IsNaN(Nu));

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

      if (!IsTruncated) return StudentT.Sample(Generator, Mu, Sigma, Nu);

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
      return StudentT.InvCDF(Mu, Sigma, Nu, p);
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

      var mu = Mu;
      var sigma = Sigma;
      var nu = Nu;

      var cdLower = StudentT.InvCDF(mu, sigma, nu, lowerP);
      var cdUpper = StudentT.InvCDF(mu, sigma, nu, upperP);

      var division = (cdUpper - cdLower) / (nDensities - 1);
      var values = Range(0, nDensities).Map(i => cdLower + i * division).ToArr();
      var densities = values.Map(v => StudentT.PDF(mu, sigma, nu, v));

      return (values, densities);
    }

    public double GetDensity(double value)
    {
      RequireTrue(IsConfigured);

      return StudentT.PDF(Mu, Sigma, Nu, value);
    }

    public override string ToString() =>
      SerializeDistribution(
        DistributionType,
        IsTruncated
          ? Array<object>(Mu, Sigma, Nu, Lower, Upper)
          : Array<object>(Mu, Sigma, Nu)
        );

    public string ToString(string variableName)
    {
      var location = IsNaN(Mu) ? "?" : Mu.ToString("G4", CurrentCulture);
      var scale = IsNaN(Sigma) ? "?" : Sigma.ToString("G4", CurrentCulture);
      var dof = IsNaN(Nu) ? "?" : Nu.ToString("G4", CurrentCulture);

      var distribution = $"{variableName} ~ T(µ = {location}, σ = {scale}, 𝜈 = {dof})";

      if (!IsTruncated) return distribution;

      var interval = "[" +
        (IsNegativeInfinity(Lower) ? "-∞" : Lower.ToString("G4", InvariantCulture)) +
        ", " +
        (IsPositiveInfinity(Upper) ? "+∞" : Upper.ToString("G4", InvariantCulture)) +
        "]";

      return $"{distribution} {interval}";
    }

    public bool Equals(StudentTDistribution rhs) =>
      Mu.Equals(rhs.Mu) && 
      Sigma.Equals(rhs.Sigma) && 
      Nu.Equals(rhs.Nu) && 
      Lower.Equals(rhs.Lower) && 
      Upper.Equals(rhs.Upper);

    public override bool Equals(object? obj)
    {
      if (obj is StudentTDistribution rhs)
      {
        return Equals(rhs);
      }

      return false;
    }

    public override int GetHashCode() => 
      HashCode.Combine(DistributionType, Mu, Sigma, Nu, Lower, Upper);

    public static bool operator ==(StudentTDistribution left, StudentTDistribution right)
    {
      return left.Equals(right);
    }

    public static bool operator !=(StudentTDistribution left, StudentTDistribution right)
    {
      return !(left == right);
    }

    internal static Option<IDistribution> Parse(Arr<string> parts)
    {
      if (parts.Count < 3) return None;
      if (!TryParse(parts[0], NumberStyles.Any, InvariantCulture, out double mu)) return None;
      if (!TryParse(parts[1], NumberStyles.Any, InvariantCulture, out double sigma)) return None;
      if (!TryParse(parts[2], NumberStyles.Any, InvariantCulture, out double nu)) return None;
      if (parts.Count != 5) return new StudentTDistribution(mu, sigma, nu);
      if (!TryParse(parts[3], NumberStyles.Any, InvariantCulture, out double lower)) return None;
      if (!TryParse(parts[4], NumberStyles.Any, InvariantCulture, out double upper)) return None;
      return new StudentTDistribution(mu, sigma, nu, lower, upper);
    }
  }
}
