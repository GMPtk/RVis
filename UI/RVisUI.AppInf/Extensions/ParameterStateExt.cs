using LanguageExt;
using RVis.Base.Extensions;
using RVis.Model;
using RVisUI.Model;
using RVisUI.Model.Extensions;
using static LanguageExt.Prelude;
using static RVis.Base.Check;

namespace RVisUI.AppInf.Extensions
{
  public static class ParameterStateExt
  {
    public static void ShareStates(
      this Arr<ParameterState> parameterStates,
      IAppState appState
      )
    {
      RequireNotNull(appState.SimSharedState);
      RequireTrue(appState.Target.IsSome);

      var sharedState = appState.SimSharedState;
      var parameters = appState.Target.AssertSome().SimConfig.SimInput.SimParameters;

      var states = parameterStates
        .Map(ps =>
        {
          var (value, minimum, maximum) =
            sharedState.ParameterSharedStates.GetParameterValueStateOrDefaults(
              ps.Name,
              parameters
              );

          var distribution = ps.GetDistribution();

          if (distribution.DistributionType != DistributionType.Invariant)
          {
            return (ps.Name, value, minimum, maximum, Some(ps.GetDistribution()));
          }

          var invariantDistribution = RequireInstanceOf<InvariantDistribution>(distribution);
          value = invariantDistribution.Value;
          if (minimum > value) minimum = value.GetPreviousOrderOfMagnitude();
          if (maximum < value) maximum = value.GetNextOrderOfMagnitude();
          return (ps.Name, value, minimum, maximum, None);
        });

      sharedState.ShareParameterState(states);
    }

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
