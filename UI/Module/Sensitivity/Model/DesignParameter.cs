using RVis.Model;

namespace Sensitivity
{
  internal struct DesignParameter
  {
    internal static DistributionType SupportedDistTypes =
        DistributionType.Beta
      | DistributionType.BetaScaled
      | DistributionType.Gamma
      | DistributionType.LogNormal
      | DistributionType.Normal
      | DistributionType.StudentT
      | DistributionType.Invariant
      | DistributionType.Uniform
      ;

    internal DesignParameter(string name, IDistribution distribution)
    {
      Name = name;
      Distribution = distribution;
    }

    internal string Name { get; }
    internal IDistribution Distribution { get; }
  }
}
