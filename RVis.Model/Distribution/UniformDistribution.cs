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
  public struct UniformDistribution : IDistribution<ContinuousUniform>, IEquatable<UniformDistribution>
  {
    public readonly static UniformDistribution Default = new UniformDistribution(NaN, NaN);

    public static UniformDistribution CreateDefault(double center)
    {
      var disp = center == 0d ? 1d : Abs(center) / 2d;
      var lower = (center - disp).ToSigFigs(2);
      var upper = (center + disp).ToSigFigs(2);
      return new UniformDistribution(lower, upper);
    }

    public UniformDistribution(double lower, double upper)
    {
      Lower = lower;
      Upper = upper;
    }

    public double Lower { get; }
    public double Upper { get; }

    public ContinuousUniform Implementation => IsConfigured
      ? new ContinuousUniform(Lower, Upper, Generator)
      : default;

    public DistributionType DistributionType => DistributionType.Uniform;

    public bool CanTruncate => false;

    public bool IsTruncated => false;

    public bool IsConfigured => !IsNaN(Lower) && !IsNaN(Upper);

    public double Mean
    {
      get
      {
        RequireTrue(IsConfigured);
        return Implementation.Mean;
      }
    }

    public double Variance
    {
      get
      {
        RequireTrue(IsConfigured);
        return Implementation.Variance;
      }
    }

    public (double LowerP, double UpperP) CumulativeDistributionAtBounds
    {
      get
      {
        var implementation = Implementation;
        var lowerP = implementation.CumulativeDistribution(Lower);
        var upperP = implementation.CumulativeDistribution(Upper);
        return (lowerP, upperP);
      }
    }

    public void FillSamples(double[] samples)
    {
      RequireTrue(IsConfigured);
      RequireNotNull(samples);

      var implementation = Implementation;
      implementation.Samples(samples);
    }

    public double GetSample()
    {
      RequireTrue(IsConfigured);
      return ContinuousUniform.Sample(Generator, Lower, Upper);
    }

    public double GetProposal(double value, double step)
    {
      if (IsNaN(value)) value = Mean;
      if (IsNaN(step)) step = Implementation.Variance;

      var sample = GetSample();
      value += (sample - value) * step;
      value = Max(Lower, Min(value, Upper));

      return value;
    }

    public double ApplyBias(double step, double bias) =>
      step * bias;

    public double InverseCumulativeDistribution(double p)
    {
      RequireTrue(IsConfigured);
      return ContinuousUniform.InvCDF(Lower, Upper, p);
    }

    public (string FunctionName, Arr<(string ArgName, double ArgValue)> FunctionParameters) RQuantileSignature =>
      ("qunif", Array(("min", Lower), ("max", Upper)));

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

      return (values, densities);
    }

    public double GetDensity(double value)
    {
      RequireTrue(IsConfigured);

      return ContinuousUniform.PDF(Lower, Upper, value);
    }

    public override string ToString() =>
      SerializeDistribution(DistributionType, Array<object>(Lower, Upper));

    public string ToString(string variableName)
    {
      var lower = IsNaN(Lower) ? "?" : Lower.ToString("G4", CurrentCulture);
      var upper = IsNaN(Upper) ? "?" : Upper.ToString("G4", CurrentCulture);

      return $"{variableName} ~ U(a = {lower}, b = {upper})";
    }

    public bool Equals(UniformDistribution rhs) =>
      Lower.Equals(rhs.Lower) && Upper.Equals(rhs.Upper);

    public override bool Equals(object obj)
    {
      if (obj is UniformDistribution rhs)
      {
        return Equals(rhs);
      }

      return false;
    }

    public override int GetHashCode()
    {
      var hashCode = -774459404;
      hashCode = hashCode * -1521134295 + DistributionType.GetHashCode();
      hashCode = hashCode * -1521134295 + Lower.GetHashCode();
      hashCode = hashCode * -1521134295 + Upper.GetHashCode();
      return hashCode;
    }

    public static bool operator ==(UniformDistribution left, UniformDistribution right)
    {
      return left.Equals(right);
    }

    public static bool operator !=(UniformDistribution left, UniformDistribution right)
    {
      return !(left == right);
    }

    internal static Option<IDistribution> Parse(Arr<string> parts)
    {
      if (parts.Count != 2) return None;
      if (!TryParse(parts[0], NumberStyles.Any, InvariantCulture, out double lower)) return None;
      if (!TryParse(parts[1], NumberStyles.Any, InvariantCulture, out double upper)) return None;
      return new UniformDistribution(lower, upper);
    }
  }
}
