using RVis.Base.Extensions;
using RVis.Model;

namespace RVisUI.AppInf.Extensions
{
  public static class ParameterStateExt
  {
    public static ParameterState WithIsSelected(this ParameterState parameterState, bool isSelected) =>
      new ParameterState(parameterState.Name, parameterState.DistributionType, parameterState.Distributions, isSelected);

    public static IDistribution GetDistribution(this ParameterState parameterState) =>
      parameterState.Distributions
        .Find(d => d.DistributionType == parameterState.DistributionType)
        .AssertSome($"Failed to find {parameterState.DistributionType} in distributions for {parameterState.Name}");

    public static IDistribution GetDistribution(this ParameterState parameterState, DistributionType distributionType) =>
      parameterState.Distributions
        .Find(d => d.DistributionType == distributionType)
        .AssertSome($"Failed to find {distributionType} in distributions for {parameterState.Name}");
  }
}
