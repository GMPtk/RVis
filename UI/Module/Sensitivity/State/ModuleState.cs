using LanguageExt;
using ReactiveUI;
using RVis.Base;
using RVis.Data;
using RVisUI.AppInf;
using RVisUI.Model.Extensions;
using System;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Sensitivity
{
  internal sealed partial class ModuleState : INotifyPropertyChanged, IDisposable
  {
    internal ParametersState ParametersState { get; } = new ParametersState();

    internal DesignState DesignState { get; } = new DesignState();

    internal TraceState TraceState { get; } = new TraceState();

    internal LowryState LowryState { get; } = new LowryState();

    internal MeasuresState MeasuresState { get; } = new MeasuresState();

    internal Arr<ParameterState> ParameterStates
    {
      get => _parameterStates;
      set => this.RaiseSetAndObserveIfChanged(
        ref _parameterStates,
        value,
        PropertyChanged,
        _parameterStateChangesSubject,
        ps => ps.Name
        );
    }
    private Arr<ParameterState> _parameterStates;

    internal SensitivityDesign? SensitivityDesign
    {
      get => _sensitivityDesign;
      set => this.RaiseAndSetIfChanged(ref _sensitivityDesign, value, PropertyChanged);
    }
    private SensitivityDesign? _sensitivityDesign;

    public NumDataTable? Trace
    {
      get => _trace;
      set => this.RaiseAndSetIfChanged(ref _trace, value, PropertyChanged);
    }
    private NumDataTable? _trace;

    public Ranking Ranking
    {
      get => _ranking;
      set => this.RaiseAndSetIfChanged(ref _ranking, value, PropertyChanged);
    }
    private Ranking _ranking;

    internal IObservable<(Arr<ParameterState> ParameterStates, ObservableQualifier ObservableQualifier)> ParameterStateChanges =>
      _parameterStateChangesSubject.AsObservable();
    private readonly ISubject<(Arr<ParameterState> ParameterStates, ObservableQualifier ObservableQualifier)> _parameterStateChangesSubject =
      new Subject<(Arr<ParameterState> ParameterStates, ObservableQualifier ObservableQualifier)>();

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
        }

        _disposed = true;
      }
    }

    private bool _disposed = false;
  }
}
