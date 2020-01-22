using LanguageExt;
using RVis.Base.Extensions;
using RVis.Model;
using RVis.Model.Extensions;
using System.ComponentModel;
using System.Linq;

namespace Plot
{
  public partial class ModuleState : INotifyPropertyChanged
  {
    public static void Save(ModuleState instance, Simulation simulation)
    {
      var dto = new _ModuleStateDTO
      {
        TraceDataPlotStates = instance.TraceDataPlotStates
            .Map(tdps => new _TraceDataPlotStateDTO
            {
              IsVisible = tdps.IsVisible,
              DepVarConfigState = new _DepVarConfigStateDTO
              {
                SelectedElementName = tdps.DepVarConfigState.SelectedElementName,
                MRUElementNames = tdps.DepVarConfigState.MRUElementNames.ToArray(),
                SupplementaryElementNames = tdps.DepVarConfigState.SupplementaryElementNames.ToArray(),
                InactiveSupplementaryElementNames = tdps.DepVarConfigState.InactiveSupplementaryElementNames
                  .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray()),
                ObservationsReferences = tdps.DepVarConfigState.ObservationsReferences.ToArray(),
                IsScaleLogarithmic = tdps.DepVarConfigState.IsScaleLogarithmic
              },
              ViewHeight = tdps.ViewHeight,
              IsSeriesTypeLine = tdps.IsSeriesTypeLine,
              IsAxesOriginLockedToZeroZero = tdps.IsAxesOriginLockedToZeroZero,
              XMinimum = tdps.XMinimum,
              XMaximum = tdps.XMaximum,
              YMinimum = tdps.YMinimum,
              YMaximum = tdps.YMaximum
            })
            .ToArray(),
        ParameterEditStates = instance.ParameterEditStates.ToArray(),
        TraceState = instance.TraceState,
        AutoApplyParameterSharedState = instance.AutoApplyParameterSharedState,
        AutoShareParameterSharedState = instance.AutoShareParameterSharedState,
        AutoApplyElementSharedState = instance.AutoApplyElementSharedState,
        AutoShareElementSharedState = instance.AutoShareElementSharedState,
        AutoApplyObservationsSharedState = instance.AutoApplyObservationsSharedState,
        AutoShareObservationsSharedState = instance.AutoShareObservationsSharedState
      };

      simulation.SavePrivateData(
        dto,
        nameof(Plot),
        nameof(ViewModel),
        nameof(ModuleState)
        );
    }

    public static ModuleState LoadOrCreate(Simulation simulation)
    {
      var maybeDTO = simulation.LoadPrivateData<_ModuleStateDTO>(
        nameof(Plot),
        nameof(ViewModel),
        nameof(ModuleState)
        );

      return maybeDTO.Match(
        dto => new ModuleState(dto, simulation),
        () => new ModuleState(simulation)
        );
    }

    private static TraceDataPlotState ToState(_TraceDataPlotStateDTO tdpsDTO)
    {
      var traceDataPlotState = new TraceDataPlotState
      {
        IsVisible = tdpsDTO.IsVisible,
        ViewHeight = tdpsDTO.ViewHeight,
        IsSeriesTypeLine = tdpsDTO.IsSeriesTypeLine,
        IsAxesOriginLockedToZeroZero = tdpsDTO.IsAxesOriginLockedToZeroZero,
        XMinimum = tdpsDTO.XMinimum,
        XMaximum = tdpsDTO.XMaximum,
        YMinimum = tdpsDTO.YMinimum,
        YMaximum = tdpsDTO.YMaximum
      };

      if (tdpsDTO.DepVarConfigState != default)
      {
        traceDataPlotState.DepVarConfigState.SelectedElementName =
          tdpsDTO.DepVarConfigState.SelectedElementName;

        traceDataPlotState.DepVarConfigState.MRUElementNames =
          tdpsDTO.DepVarConfigState.MRUElementNames.IsNullOrEmpty()
            ? default
            : tdpsDTO.DepVarConfigState.MRUElementNames.ToArr();

        traceDataPlotState.DepVarConfigState.SupplementaryElementNames =
          tdpsDTO.DepVarConfigState.SupplementaryElementNames.IsNullOrEmpty()
            ? default
            : tdpsDTO.DepVarConfigState.SupplementaryElementNames.ToArr();

        if (tdpsDTO.DepVarConfigState.InactiveSupplementaryElementNames != default)
        {
          foreach (var kvp in tdpsDTO.DepVarConfigState.InactiveSupplementaryElementNames)
          {
            traceDataPlotState.DepVarConfigState.InactiveSupplementaryElementNames.Add(kvp.Key, kvp.Value.ToArr());
          }
        }

        traceDataPlotState.DepVarConfigState.ObservationsReferences =
          tdpsDTO.DepVarConfigState.ObservationsReferences.IsNullOrEmpty()
            ? default
            : tdpsDTO.DepVarConfigState.ObservationsReferences.ToArr();

        traceDataPlotState.DepVarConfigState.IsScaleLogarithmic = tdpsDTO.DepVarConfigState.IsScaleLogarithmic;
      }

      return traceDataPlotState;
    }

    private ModuleState(_ModuleStateDTO dto, Simulation simulation)
    {
      TraceDataPlotStates = dto.TraceDataPlotStates.IsNullOrEmpty()
        ? DefaultTraceDataPlotStates
        : dto.TraceDataPlotStates.Select(ToState).ToArr();

      var parameterEditStates = dto.ParameterEditStates.IsNullOrEmpty()
        ? default
        : dto.ParameterEditStates.ToArr();

      parameterEditStates = simulation.SimConfig.SimInput.SimParameters.Map(p =>
      {
        var maybeExistingState = parameterEditStates.Find(pes => pes.Name == p.Name);
        return maybeExistingState.Match(
          es => es,
          () => new ParameterEditState(p.Name, p.Scalar));
      });

      ParameterEditStates = parameterEditStates;

      TraceState = dto.TraceState ?? new TraceState();

      _autoApplyParameterSharedState = dto.AutoApplyParameterSharedState;
      _autoShareParameterSharedState = dto.AutoShareParameterSharedState;
      _autoApplyElementSharedState = dto.AutoApplyElementSharedState;
      _autoShareElementSharedState = dto.AutoShareElementSharedState;
      _autoApplyObservationsSharedState = dto.AutoApplyObservationsSharedState;
      _autoShareObservationsSharedState = dto.AutoShareObservationsSharedState;
    }

    private ModuleState(Simulation simulation) : this(new _ModuleStateDTO(), simulation)
    {
    }
  }
}
