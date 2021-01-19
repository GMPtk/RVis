using LanguageExt;
using ReactiveUI;
using RVis.Base;
using RVis.Base.Extensions;
using RVis.Model;
using RVis.Model.Extensions;
using RVisUI.AppInf;
using RVisUI.Model;
using RVisUI.Model.Extensions;
using System;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using static LanguageExt.Prelude;

namespace Estimation
{
  internal sealed partial class ModuleState : INotifyPropertyChanged, IDisposable
  {
    internal PriorsState PriorsState { get; } = new PriorsState();

    internal LikelihoodState LikelihoodState { get; } = new LikelihoodState();

    internal DesignState DesignState { get; } = new DesignState();

    internal SimulationState SimulationState { get; } = new SimulationState();

    internal Arr<ParameterState> PriorStates
    {
      get => _priorStates;
      set => this.RaiseSetAndObserveIfChanged(
        ref _priorStates,
        value,
        PropertyChanged,
        _priorStateChangesSubject,
        ps => ps.Name
        );
    }
    private Arr<ParameterState> _priorStates;

    internal IObservable<(Arr<ParameterState> PriorStates, ObservableQualifier ObservableQualifier)> PriorStateChanges =>
      _priorStateChangesSubject.AsObservable();
    private readonly ISubject<(Arr<ParameterState> PriorStates, ObservableQualifier ObservableQualifier)> _priorStateChangesSubject =
      new Subject<(Arr<ParameterState> PriorStates, ObservableQualifier ObservableQualifier)>();

    internal Arr<OutputState> OutputStates
    {
      get => _outputStates;
      set => this.RaiseSetAndObserveIfChanged(
        ref _outputStates,
        value,
        PropertyChanged,
        _outputStateChangesSubject,
        os => os.Name
        );
    }
    private Arr<OutputState> _outputStates;

    internal IObservable<(Arr<OutputState> OutputStates, ObservableQualifier ObservableQualifier)> OutputStateChanges =>
      _outputStateChangesSubject.AsObservable();
    private readonly ISubject<(Arr<OutputState> OutputStates, ObservableQualifier ObservableQualifier)> _outputStateChangesSubject =
      new Subject<(Arr<OutputState> OutputStates, ObservableQualifier ObservableQualifier)>();

    public Arr<SimObservations> SelectedObservations
    {
      get => _selectedObservations;
      set => this.RaiseSetAndObserveIfChanged(
        ref _selectedObservations,
        value,
        PropertyChanged,
        _observationsChangesSubject,
        o => o.ID
        );
    }
    private Arr<SimObservations> _selectedObservations;

    public IObservable<(Arr<SimObservations> Observations, ObservableQualifier ObservableQualifier)> ObservationsChanges =>
      _observationsChangesSubject.AsObservable();
    private readonly ISubject<(Arr<SimObservations> Observations, ObservableQualifier ObservableQualifier)> _observationsChangesSubject =
      new Subject<(Arr<SimObservations> Observations, ObservableQualifier ObservableQualifier)>();

    public void SelectObservations(SimObservations observations) =>
      SelectObservations(Array(observations));

    public void SelectObservations(Arr<SimObservations> observations)
    {
      var toSelect = observations.Filter(o => !SelectedObservations.ContainsObservations(o));
      if (toSelect.IsEmpty) return;

      SelectedObservations = SelectedObservations.AddRange(toSelect);
    }

    public void UnselectObservations(SimObservations observations) =>
      UnselectObservations(Array(observations));

    public void UnselectObservations(Arr<SimObservations> observations)
    {
      var toUnselect = observations.Filter(o => SelectedObservations.ContainsObservations(o));
      if (toUnselect.IsEmpty) return;

      SelectedObservations = SelectedObservations.Filter(o => !observations.ContainsObservations(o));
    }

    internal EstimationDesign? EstimationDesign
    {
      get => _estimationDesign;
      set => this.RaiseAndSetIfChanged(ref _estimationDesign, value, PropertyChanged);
    }
    private EstimationDesign? _estimationDesign;

    internal Arr<ChainState> ChainStates
    {
      get => _chainStates;
      set => this.RaiseAndSetIfChanged(ref _chainStates, value, PropertyChanged);
    }
    private Arr<ChainState> _chainStates;

    internal PosteriorState? PosteriorState
    {
      get => _posteriorState;
      set => this.RaiseAndSetIfChanged(ref _posteriorState, value, PropertyChanged);
    }
    private PosteriorState? _posteriorState;

    internal string? RootExportDirectory
    {
      get => _rootExportDirectory;
      set => this.RaiseAndSetIfChanged(ref _rootExportDirectory, value, PropertyChanged);
    }
    private string? _rootExportDirectory;

    internal bool OpenAfterExport
    {
      get => _openAfterExport;
      set => this.RaiseAndSetIfChanged(ref _openAfterExport, value, PropertyChanged);
    }
    private bool _openAfterExport;

    internal bool? AutoApplyParameterSharedState
    {
      get => _autoApplyParameterSharedState;
      set => this.RaiseAndSetIfChanged(ref _autoApplyParameterSharedState, value, PropertyChanged);
    }
    private bool? _autoApplyParameterSharedState;

    internal bool? AutoShareParameterSharedState
    {
      get => _autoShareParameterSharedState;
      set => this.RaiseAndSetIfChanged(ref _autoShareParameterSharedState, value, PropertyChanged);
    }
    private bool? _autoShareParameterSharedState;

    internal bool? AutoApplyElementSharedState
    {
      get => _autoApplyElementSharedState;
      set => this.RaiseAndSetIfChanged(ref _autoApplyElementSharedState, value, PropertyChanged);
    }
    private bool? _autoApplyElementSharedState;

    internal bool? AutoShareElementSharedState
    {
      get => _autoShareElementSharedState;
      set => this.RaiseAndSetIfChanged(ref _autoShareElementSharedState, value, PropertyChanged);
    }
    private bool? _autoShareElementSharedState;

    internal bool? AutoApplyObservationsSharedState
    {
      get => _autoApplyObservationsSharedState;
      set => this.RaiseAndSetIfChanged(ref _autoApplyObservationsSharedState, value, PropertyChanged);
    }
    private bool? _autoApplyObservationsSharedState;

    internal bool? AutoShareObservationsSharedState
    {
      get => _autoShareObservationsSharedState;
      set => this.RaiseAndSetIfChanged(ref _autoShareObservationsSharedState, value, PropertyChanged);
    }
    private bool? _autoShareObservationsSharedState;

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Dispose() => Dispose(true);

    private void Dispose(bool disposing)
    {
      if (!_disposed)
      {
        if (disposing)
        {
          _subscriptions?.Dispose();
        }

        _disposed = true;
      }
    }

    private void ObserveEvidenceObservationsChanges((Arr<SimObservations> Observations, ObservableQualifier ObservableQualifier) change)
    {
      if (change.ObservableQualifier.IsRemove())
      {
        var toUnselect = SelectedObservations.Filter(
          o => change.Observations.ContainsObservations(o)
          );

        UnselectObservations(toUnselect);
      }
    }

    private readonly EstimationDesigns _estimationDesigns;
    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
    private readonly IDisposable _subscriptions;
    private bool _disposed = false;
  }
}
