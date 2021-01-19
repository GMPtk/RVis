using LanguageExt;
using System;
using System.Globalization;
using static LanguageExt.Prelude;
using static RVis.Base.Check;
using static RVis.Model.Distribution;
using static System.Double;
using static System.Globalization.CultureInfo;

namespace RVis.Model
{
  public struct InvariantDistribution : IDistribution, IEquatable<InvariantDistribution>
  {
    public readonly static InvariantDistribution Default = new InvariantDistribution(NaN);

    public InvariantDistribution(double value)
      => Value = value;

    public double Value { get; }

    public DistributionType DistributionType => DistributionType.Invariant;

    public bool CanTruncate => false;

    public bool IsTruncated => false;

    public IDistribution WithLowerUpper(double lower, double upper) =>
      this;

    public bool IsConfigured => !IsNaN(Value);

    public double Mean
    {
      get
      {
        RequireTrue(IsConfigured);
        return Value;
      }
    }

    public double Variance
    {
      get
      {
        RequireTrue(IsConfigured);
        return 0d;
      }
    }

    public (double LowerP, double UpperP) CumulativeDistributionAtBounds =>
      throw new InvalidOperationException("Not a continuous random variable");

    public void FillSamples(double[] samples)
    {
      RequireTrue(IsConfigured);
      RequireNotNull(samples);

      for (var i = 0; i < samples.Length; ++i) samples[i] = Value;
    }

    public double GetSample()
    {
      RequireTrue(IsConfigured);
      return Value;
    }

    public double GetProposal(double value, double step) =>
      value;

    public double ApplyBias(double step, double bias) =>
      step;

    public double InverseCumulativeDistribution(double p) =>
      throw new InvalidOperationException("Not a continuous random variable");

    public (string? FunctionName, Arr<(string ArgName, double ArgValue)> FunctionParameters) RQuantileSignature =>
      default;

    public (string FunctionName, Arr<(string ArgName, double ArgValue)> FunctionParameters) RInverseTransformSamplingSignature =>
      default;

    public (Arr<double> Values, Arr<double> Densities) GetDensities(double lowerP, double upperP, int nDensities) =>
      throw new InvalidOperationException("Not a continuous random variable");

    public double GetDensity(double value) =>
      throw new InvalidOperationException("Not a continuous random variable");

    public override string ToString() =>
      SerializeDistribution(DistributionType, Array<object>(Value));

    public string ToString(string variableName)
    {
      var value = IsNaN(Value) ? "?" : Value.ToString("G4", CurrentCulture);

      return $"{variableName} = {value}";
    }

    public bool Equals(InvariantDistribution rhs) =>
      Value.Equals(rhs.Value);

    public override bool Equals(object? obj)
    {
      if (obj is InvariantDistribution rhs)
      {
        return Equals(rhs);
      }

      return false;
    }

    public override int GetHashCode() => 
      HashCode.Combine(DistributionType, Value);

    public static bool operator ==(InvariantDistribution left, InvariantDistribution right)
    {
      return left.Equals(right);
    }

    public static bool operator !=(InvariantDistribution left, InvariantDistribution right)
    {
      return !(left == right);
    }

    internal static Option<IDistribution> Parse(Arr<string> parts)
    {
      if (parts.Count != 1) return None;
      if (!TryParse(parts[0], NumberStyles.Any, InvariantCulture, out double value)) return None;
      return new InvariantDistribution(value);
    }
  }
}
