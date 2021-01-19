using LanguageExt;
using RVisUI.Model.Extensions;
using System.ComponentModel;
using static LanguageExt.Prelude;

namespace Plot
{
  public partial class ModuleState : INotifyPropertyChanged
  {
    public const int N_TRACES = 4;

    public static Arr<TraceDataPlotState> DefaultTraceDataPlotStates =>
      Range(1, N_TRACES)
      .Map(i => new TraceDataPlotState { IsVisible = i == 1 })
      .ToArr();

    public Arr<TraceDataPlotState> TraceDataPlotStates { get; }

    public Arr<ParameterEditState> ParameterEditStates { get; }

    public TraceState TraceState { get; }

    public bool? AutoApplyParameterSharedState
    {
      get => _autoApplyParameterSharedState;
      set => this.RaiseAndSetIfChanged(ref _autoApplyParameterSharedState, value, PropertyChanged);
    }
    private bool? _autoApplyParameterSharedState;

    public bool? AutoShareParameterSharedState
    {
      get => _autoShareParameterSharedState;
      set => this.RaiseAndSetIfChanged(ref _autoShareParameterSharedState, value, PropertyChanged);
    }
    private bool? _autoShareParameterSharedState;

    public bool? AutoApplyElementSharedState
    {
      get => _autoApplyElementSharedState;
      set => this.RaiseAndSetIfChanged(ref _autoApplyElementSharedState, value, PropertyChanged);
    }
    private bool? _autoApplyElementSharedState;

    public bool? AutoShareElementSharedState
    {
      get => _autoShareElementSharedState;
      set => this.RaiseAndSetIfChanged(ref _autoShareElementSharedState, value, PropertyChanged);
    }
    private bool? _autoShareElementSharedState;

    public bool? AutoApplyObservationsSharedState
    {
      get => _autoApplyObservationsSharedState;
      set => this.RaiseAndSetIfChanged(ref _autoApplyObservationsSharedState, value, PropertyChanged);
    }
    private bool? _autoApplyObservationsSharedState;

    public bool? AutoShareObservationsSharedState
    {
      get => _autoShareObservationsSharedState;
      set => this.RaiseAndSetIfChanged(ref _autoShareObservationsSharedState, value, PropertyChanged);
    }
    private bool? _autoShareObservationsSharedState;

    public event PropertyChangedEventHandler? PropertyChanged;
  }
}
