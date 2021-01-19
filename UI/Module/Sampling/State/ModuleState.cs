using LanguageExt;
using RVis.Base;
using RVis.Data;
using RVisUI.AppInf;
using RVisUI.Model.Extensions;
using System;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using static RVis.Base.Check;
using static RVisUI.Wpf.WpfTools;
using DataTable = System.Data.DataTable;

namespace Sampling
{
  internal sealed partial class ModuleState : INotifyPropertyChanged, IDisposable
  {
    internal ModuleState()
    {
      RequireTrue(IsInDesignMode);
    }

    internal ParametersState ParametersState { get; } = new ParametersState();

    internal SamplesState SamplesState { get; } = new SamplesState();

    internal OutputsState OutputsState { get; } = new OutputsState();

    internal FilteredSamplesState FilteredSamplesState { get; } = new FilteredSamplesState { IsUnion = true };

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

    internal IObservable<(Arr<ParameterState> ParameterStates, ObservableQualifier ObservableQualifier)> ParameterStateChanges =>
      _parameterStateChangesSubject.AsObservable();
    private readonly ISubject<(Arr<ParameterState> ParameterStates, ObservableQualifier ObservableQualifier)> _parameterStateChangesSubject =
      new Subject<(Arr<ParameterState> ParameterStates, ObservableQualifier ObservableQualifier)>();

    internal DataTable? Samples
    {
      get => _samples;
      set => this.RaiseAndSetIfChanged(ref _samples, value, PropertyChanged);
    }
    private DataTable? _samples;

    internal Arr<(int Index, NumDataTable Output)> Outputs
    {
      get => _outputs;
      set => this.RaiseAndSetIfChanged(ref _outputs, value, PropertyChanged);
    }
    private Arr<(int Index, NumDataTable Output)> _outputs;

    internal FilterConfig FilterConfig
    {
      get => _filterConfig;
      set => this.RaiseAndSetIfChanged(ref _filterConfig, value, PropertyChanged);
    }
    private FilterConfig _filterConfig = FilterConfig.Default;

    internal Arr<(int Index, bool IsInFilteredSet)> OutputFilters
    {
      get => _outputFilters;
      set => this.RaiseAndSetIfChanged(ref _outputFilters, value, PropertyChanged);
    }
    private Arr<(int Index, bool IsInFilteredSet)> _outputFilters;

    internal SamplingDesign? SamplingDesign
    {
      get => _samplingDesign;
      set => this.RaiseAndSetIfChanged(ref _samplingDesign, value, PropertyChanged);
    }
    private SamplingDesign? _samplingDesign;

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
