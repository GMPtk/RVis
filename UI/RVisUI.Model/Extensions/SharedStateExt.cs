using LanguageExt;
using RVis.Base.Extensions;
using RVis.Model;
using RVis.Model.Extensions;
using static LanguageExt.Prelude;

namespace RVisUI.Model.Extensions
{
  public static class SharedStateExt
  {
    public static bool ContainsState(this Arr<SimParameterSharedState> parameterSharedStates, string name) =>
      parameterSharedStates.Exists(pss => pss.Name == name);

    public static Option<SimParameterSharedState> FindState(this Arr<SimParameterSharedState> parameterSharedStates, string name) =>
      parameterSharedStates.Find(pss => pss.Name == name);

    public static Option<SimParameterSharedState> FindState(this Arr<SimParameterSharedState> parameterSharedStates, SimParameterSharedState parameterSharedState) =>
      FindState(parameterSharedStates, parameterSharedState.Name);

    public static (double Value, double Minimum, double Maximum) GetParameterValueStateOrDefaults(
      this Arr<SimParameterSharedState> parameterSharedStates, 
      string name, 
      Arr<SimParameter> parameters
      )
    {
      var parameterSharedState = parameterSharedStates.Find(pss => pss.Name == name);

      return parameterSharedState.Match(
        pss => (pss.Value, pss.Minimum, pss.Maximum),
        () =>
        {
          var parameter = parameters.GetParameter(name);
          var scalar = parameter.Scalar;
          return (scalar, scalar.GetPreviousOrderOfMagnitude(), scalar.GetNextOrderOfMagnitude());
        });
    }

    public static bool ContainsState(this Arr<SimElementSharedState> elementSharedStates, string name) =>
      elementSharedStates.Exists(ess => ess.Name == name);

    public static Option<SimElementSharedState> FindState(this Arr<SimElementSharedState> elementSharedStates, string name) =>
      elementSharedStates.Find(ess => ess.Name == name);

    public static Option<SimElementSharedState> FindState(this Arr<SimElementSharedState> elementSharedStates, SimElementSharedState elementSharedState) =>
      FindState(elementSharedStates, elementSharedState.Name);

    public static bool ContainsState(this Arr<SimObservationsSharedState> observationsSharedStates, string reference) =>
      observationsSharedStates.Exists(oss => oss.Reference == reference);

    public static Option<SimObservationsSharedState> FindState(this Arr<SimObservationsSharedState> observationsSharedStates, string reference) =>
      observationsSharedStates.Find(oss => oss.Reference == reference);

    public static Option<SimObservationsSharedState> FindState(this Arr<SimObservationsSharedState> observationsSharedStates, SimObservationsSharedState observationsSharedState) =>
      FindState(observationsSharedStates, observationsSharedState.Reference);

    public static bool IncludesParameters(this SimSharedStateBuild sharedStateBuild) =>
      (sharedStateBuild & SimSharedStateBuild.Parameters) != 0;
    public static bool IncludesOutputs(this SimSharedStateBuild sharedStateBuild) =>
      (sharedStateBuild & SimSharedStateBuild.Outputs) != 0;
    public static bool IncludesObservations(this SimSharedStateBuild sharedStateBuild) =>
      (sharedStateBuild & SimSharedStateBuild.Observations) != 0;

    public static bool IncludesParameters(this SimSharedStateApply sharedStateApply) =>
      (sharedStateApply & SimSharedStateApply.Parameters) != 0;
    public static bool IncludesOutputs(this SimSharedStateApply sharedStateApply) =>
      (sharedStateApply & SimSharedStateApply.Outputs) != 0;
    public static bool IncludesObservations(this SimSharedStateApply sharedStateApply) =>
      (sharedStateApply & SimSharedStateApply.Observations) != 0;

    public static bool IsSingle(this SimSharedStateApply sharedStateApply) =>
      (sharedStateApply & SimSharedStateApply.Single) != 0;
    public static bool IsSet(this SimSharedStateApply sharedStateApply) =>
      (sharedStateApply & SimSharedStateApply.Set) != 0;

    public static void ShareParameterState(
      this ISimSharedState sharedState, 
      string name, 
      double value, 
      double minimum, 
      double maximum,
      Option<IDistribution> distribution
      ) =>
      sharedState.ShareParameterState(Array((name, value, minimum, maximum, distribution)));

    public static void UnshareParameterState(this ISimSharedState sharedState, string name) =>
      sharedState.UnshareParameterState(Array(name));

    public static void ShareElementState(this ISimSharedState sharedState, string name) =>
      sharedState.ShareElementState(Array(name));

    public static void UnshareElementState(this ISimSharedState sharedState, string name) =>
      sharedState.UnshareElementState(Array(name));

    public static void ShareObservationsState(this ISimSharedState sharedState, string reference) =>
      sharedState.ShareObservationsState(Array(reference));

    public static void UnshareObservationsState(this ISimSharedState sharedState, string reference) =>
      sharedState.UnshareObservationsState(Array(reference));
  }
}
