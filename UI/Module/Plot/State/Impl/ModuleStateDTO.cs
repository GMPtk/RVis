using System.Collections.Generic;

namespace Plot
{
  internal class _DepVarConfigStateDTO
  {
    public string? SelectedElementName { get; set; }
    public string[]? MRUElementNames { get; set; }
    public string? SelectedInsetElementName { get; set; }
    public string[]? SupplementaryElementNames { get; set; }
    public Dictionary<string, string[]>? InactiveSupplementaryElementNames { get; set; }
    public string[]? ObservationsReferences { get; set; }
    public bool IsScaleLogarithmic { get; set; }
  }

  internal class _TraceDataPlotStateDTO
  {
    public bool IsVisible { get; set; }
    public _DepVarConfigStateDTO? DepVarConfigState { get; set; }
    public double ViewHeight { get; set; }
    public bool IsSeriesTypeLine { get; set; }
    public bool IsAxesOriginLockedToZeroZero { get; set; }
    public double? XMinimum { get; set; }
    public double? XMaximum { get; set; }
    public double? YMinimum { get; set; }
    public double? YMaximum { get; set; }
  }

  internal class _ModuleStateDTO
  {
    public _TraceDataPlotStateDTO[]? TraceDataPlotStates { get; set; }
    public ParameterEditState[]? ParameterEditStates { get; set; }
    public TraceState? TraceState { get; set; }
    public bool? AutoApplyParameterSharedState { get; set; } = false;
    public bool? AutoShareParameterSharedState { get; set; } = true;
    public bool? AutoApplyElementSharedState { get; set; } = false;
    public bool? AutoShareElementSharedState { get; set; } = true;
    public bool? AutoApplyObservationsSharedState { get; set; } = false;
    public bool? AutoShareObservationsSharedState { get; set; } = false;
  }

}
