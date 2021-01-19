using LanguageExt;
using RVis.Model;
using System;

namespace RVisUI.Model
{
  public readonly struct SimParameterSharedState : IEquatable<SimParameterSharedState>
  {
    public SimParameterSharedState(string name, double value, double minimum, double maximum, Option<IDistribution> distribution)
    {
      Name = name;
      Value = value;
      Minimum = minimum;
      Maximum = maximum;
      Distribution = distribution;
    }

    public string Name { get; }

    public double Value { get; }

    public double Minimum { get; }

    public double Maximum { get; }

    public Option<IDistribution> Distribution { get; }

    public override bool Equals(object? obj) =>
      obj is SimParameterSharedState parameterSharedState && Equals(parameterSharedState);

    public bool Equals(SimParameterSharedState other) =>
      Name == other.Name &&
      Value.Equals(other.Value) &&
      Minimum.Equals(other.Minimum) &&
      Maximum.Equals(other.Maximum) &&
      Distribution == other.Distribution;

    public override int GetHashCode() => 
      HashCode.Combine(Name, Value, Minimum, Maximum, Distribution);

    public static bool operator ==(SimParameterSharedState lhs, SimParameterSharedState rhs) =>
      lhs.Equals(rhs);

    public static bool operator !=(SimParameterSharedState lhs, SimParameterSharedState rhs) =>
      !(lhs == rhs);
  }
}
