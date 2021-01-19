using LanguageExt;
using RVis.Base.Extensions;
using RVis.Model;
using System;
using static RVis.Base.Check;

namespace RVisUI.AppInf
{
  public struct ParameterState : IEquatable<ParameterState>
  {
    public static ParameterState Create(string name)
    {
      var distributions = Distribution.GetDefaults();

      var parameterState = new ParameterState(
        name,
        DistributionType.Normal,
        distributions,
        true
        );

      return parameterState;
    }

    public static ParameterState Create(string name, double value)
    {
      var distributions = Distribution.GetDefaults();

      var indexDistribution = distributions.FindIndex(d => d.DistributionType == DistributionType.Normal);
      RequireTrue(indexDistribution.IsFound());
      IDistribution distribution = NormalDistribution.CreateDefault(value);
      distributions = distributions.SetItem(
        indexDistribution,
        distribution
        );

      indexDistribution = distributions.FindIndex(d => d.DistributionType == DistributionType.Uniform);
      RequireTrue(indexDistribution.IsFound());
      distribution = UniformDistribution.CreateDefault(value);
      distributions = distributions.SetItem(
        indexDistribution,
        distribution
        );

      var parameterState = new ParameterState(
        name,
        DistributionType.Normal,
        distributions,
        true
        );

      return parameterState;
    }

    public ParameterState(
      string name,
      DistributionType distributionType,
      Arr<IDistribution> distributions,
      bool isSelected
      )
    {
      RequireNotNullEmptyWhiteSpace(name);
      RequireFalse(distributionType == DistributionType.None);
      RequireTrue(distributions.Exists(d => d.DistributionType == distributionType));

      Name = name;
      DistributionType = distributionType;
      Distributions = distributions;
      IsSelected = isSelected;
    }

    public string Name { get; }

    public DistributionType DistributionType { get; }

    public Arr<IDistribution> Distributions { get; }

    public bool IsSelected { get; }

    public override bool Equals(object? obj)
    {
      if (obj is ParameterState rhs) return Equals(rhs);
      return false;
    }

    public bool Equals(ParameterState rhs) =>
      Name == rhs.Name &&
      DistributionType == rhs.DistributionType &&
      Distributions == rhs.Distributions &&
      IsSelected == rhs.IsSelected;

    public override int GetHashCode() =>
      HashCode.Combine(Name, DistributionType, Distributions, IsSelected);

    public static bool operator ==(ParameterState left, ParameterState right) =>
      left.Equals(right);

    public static bool operator !=(ParameterState left, ParameterState right) =>
      !(left == right);
  }
}
