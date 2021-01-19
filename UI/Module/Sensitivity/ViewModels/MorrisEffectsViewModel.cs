using LanguageExt;
using ReactiveUI;
using RVis.Base.Extensions;
using RVis.Model;
using RVis.Model.Extensions;
using RVisUI.AppInf.Extensions;
using RVisUI.Model;
using RVisUI.Model.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reactive.Disposables;
using System.Windows.Input;
using System.Windows.Threading;
using static LanguageExt.Prelude;
using static RVis.Base.Check;
using static RVis.Base.Extensions.NumExt;
using static Sensitivity.MuStarSigmaPlotData;
using static System.Double;

namespace Sensitivity
{
  internal sealed class MorrisEffectsViewModel : IMorrisEffectsViewModel, INotifyPropertyChanged, IDisposable
  {
    internal MorrisEffectsViewModel(
      IAppState appState,
      IAppService appService,
      IAppSettings appSettings,
      ModuleState moduleState
      )
    {
      _appState = appState;
      _simulation = appState.Target.AssertSome();
      _moduleState = moduleState;

      var independentVariable = _simulation.SimConfig.SimOutput.IndependentVariable;
      XUnits = independentVariable.Unit;

      _muStarSigmaViewModel = new MuStarSigmaViewModel(appService, appSettings);
      _traceViewModel = new TraceViewModel(appService, appSettings, moduleState.TraceState);

      _playTicker = new DispatcherTimer(DispatcherPriority.Normal);
      _playTicker.Tick += HandlePlayTick;
      PlaySpeed = _playSpeeds[_playSpeedIndex];

      PlaySimulation = ReactiveCommand.Create(
        HandlePlaySimulation,
        this.ObservableForProperty(vm => vm.CanPlaySimulation, _ => CanPlaySimulation)
        );

      StopSimulation = ReactiveCommand.Create(
        HandleStopSimulation,
        this.ObservableForProperty(vm => vm.CanStopSimulation, _ => CanStopSimulation)
        );

      PlaySlower = ReactiveCommand.Create(
        HandlePlaySlower,
        this.ObservableForProperty(vm => vm.CanPlaySlower, _ => CanPlaySlower)
        );

      PlayFaster = ReactiveCommand.Create(
        HandlePlayFaster,
        this.ObservableForProperty(vm => vm.CanPlayFaster, _ => CanPlayFaster)
        );

      UseRankedParameters = ReactiveCommand.Create(
        HandleUseRankedParameters,
        this.ObservableForProperty(vm => vm.RankedParameterViewModels, _ => RankedParameterViewModels.Count > 0)
        );

      ShareRankedParameters = ReactiveCommand.Create(
        HandleShareRankedParameters,
        this.ObservableForProperty(vm => vm.RankedParameterViewModels, _ => RankedParameterViewModels.Count > 0)
        );

      _reactiveSafeInvoke = appService.GetReactiveSafeInvoke();

      _subscriptions = new CompositeDisposable(

        moduleState
          .ObservableForProperty(ms => ms.SensitivityDesign)
          .Subscribe(ObserveModuleStateSensitivityDesign),

        moduleState.MeasuresState
          .ObservableForProperty(ms => ms.MorrisOutputMeasures)
          .Subscribe(ObserveMeasuresStateMorrisOutputMeasures),

        moduleState.MeasuresState
          .ObservableForProperty(ms => ms.SelectedOutputName)
          .Subscribe(ObserveMeasuresStateSelectedOutputName),

        moduleState
          .ObservableForProperty(ms => ms.Ranking)
          .Subscribe(ObserveModuleStateRanking),

        this
          .ObservableForProperty(vm => vm.IsVisible)
          .Subscribe(ObserveIsVisible),
        
        this
          .ObservableForProperty(vm => vm.SelectedOutputName)
          .Subscribe(ObserveSelectedOutputName),

        _traceViewModel
          .ObservableForProperty(vm => vm.SelectedX)
          .Subscribe(ObserveTraceSelectedX)

        );

      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        Populate();
        PopulateRanking();
        UpdateEnable();
      }
    }

    public bool IsVisible
    {
      get => _isVisible;
      set => this.RaiseAndSetIfChanged(ref _isVisible, value, PropertyChanged);
    }
    private bool _isVisible;

    public bool IsReady
    {
      get => _isReady;
      set => this.RaiseAndSetIfChanged(ref _isReady, value, PropertyChanged);
    }
    private bool _isReady;

    public IMuStarSigmaViewModel MuStarSigmaViewModel => _muStarSigmaViewModel;

    public ITraceViewModel TraceViewModel => _traceViewModel;

    public Arr<string> OutputNames
    {
      get => _outputNames;
      set => this.RaiseAndSetIfChanged(ref _outputNames, value, PropertyChanged);
    }
    private Arr<string> _outputNames;

    public bool CanSelectOutputName
    {
      get => _canSelectOutputName;
      set => this.RaiseAndSetIfChanged(ref _canSelectOutputName, value, PropertyChanged);
    }
    private bool _canSelectOutputName;

    public int SelectedOutputName
    {
      get => _selectedOutputName;
      set => this.RaiseAndSetIfChanged(ref _selectedOutputName, value, PropertyChanged);
    }
    private int _selectedOutputName = NOT_FOUND;

    public int PlaySpeed
    {
      get => _playSpeed;
      set => this.RaiseAndSetIfChanged(ref _playSpeed, value, PropertyChanged);
    }
    private int _playSpeed;

    public ICommand PlaySimulation { get; }

    public bool CanPlaySimulation
    {
      get => _canPlaySimulation;
      set => this.RaiseAndSetIfChanged(ref _canPlaySimulation, value, PropertyChanged);
    }
    private bool _canPlaySimulation;

    public ICommand StopSimulation { get; }

    public bool CanStopSimulation
    {
      get => _canStopSimulation;
      set => this.RaiseAndSetIfChanged(ref _canStopSimulation, value, PropertyChanged);
    }
    private bool _canStopSimulation;

    public ICommand PlaySlower { get; }

    public bool CanPlaySlower
    {
      get => _canPlaySlower;
      set => this.RaiseAndSetIfChanged(ref _canPlaySlower, value, PropertyChanged);
    }
    private bool _canPlaySlower;

    public ICommand PlayFaster { get; }

    public bool CanPlayFaster
    {
      get => _canPlayFaster;
      set => this.RaiseAndSetIfChanged(ref _canPlayFaster, value, PropertyChanged);
    }
    private bool _canPlayFaster;

    public string? XUnits { get; }

    public Arr<IRankedParameterViewModel> RankedParameterViewModels
    {
      get => _rankedParameterViewModels;
      set => this.RaiseAndSetIfChanged(ref _rankedParameterViewModels, value, PropertyChanged);
    }
    private Arr<IRankedParameterViewModel> _rankedParameterViewModels;

    public Arr<string> RankedUsing
    {
      get => _rankedUsing;
      set => this.RaiseAndSetIfChanged(ref _rankedUsing, value, PropertyChanged);
    }
    private Arr<string> _rankedUsing;

    public double? RankedFrom
    {
      get => _rankedFrom;
      set => this.RaiseAndSetIfChanged(ref _rankedFrom, value, PropertyChanged);
    }
    private double? _rankedFrom;

    public double? RankedTo
    {
      get => _rankedTo;
      set => this.RaiseAndSetIfChanged(ref _rankedTo, value, PropertyChanged);
    }
    private double? _rankedTo;

    public ICommand UseRankedParameters { get; }

    public ICommand ShareRankedParameters { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Dispose() =>
      Dispose(disposing: true);

    private void Dispose(bool disposing)
    {
      if (!_disposed)
      {
        if (disposing)
        {
          _subscriptions.Dispose();
          _traceViewModel.Dispose();
          _muStarSigmaViewModel.Dispose();
        }

        _disposed = true;
      }
    }

    private void UpdateEnable()
    {
      CanSelectOutputName = !_outputNames.IsEmpty && !_playTicker.IsEnabled;
      CanPlaySimulation =
        !_playTicker.IsEnabled &&
        _traceViewModel.SelectedX < _traceViewModel.XValues.LastOrDefault();
      CanStopSimulation = _playTicker.IsEnabled;
      CanPlaySlower = _playTicker.IsEnabled && _playSpeedIndex > 0;
      CanPlayFaster = _playTicker.IsEnabled && _playSpeedIndex < _playSpeeds.Count - 1;
    }

    private void HandlePlayTick(object? sender, EventArgs e)
    {
      var xs = _traceViewModel.XValues;
      var index = xs.IndexOf(_traceViewModel.SelectedX);
      if (index < xs.Count - 1)
      {
        ++index;
        _traceViewModel.SelectedX = xs[index];
      }
      else
      {
        _playTicker.Stop();
        UpdateEnable();
      }
    }

    private void HandlePlaySimulation()
    {
      _playTicker.Interval = TimeSpan.FromSeconds(1.0 / PlaySpeed);
      _playTicker.Start();
      UpdateEnable();
    }

    private void HandleStopSimulation()
    {
      _playTicker.Stop();
      UpdateEnable();
    }

    private void HandlePlaySlower()
    {
      if (_playSpeedIndex > 0)
      {
        _playTicker.Stop();
        --_playSpeedIndex;
        PlaySpeed = _playSpeeds[_playSpeedIndex];
        _playTicker.Interval = TimeSpan.FromSeconds(1.0 / PlaySpeed);
        _playTicker.Start();
        UpdateEnable();
      }
    }

    private void HandlePlayFaster()
    {
      if (_playSpeedIndex < _playSpeeds.Count - 1)
      {
        _playTicker.Stop();
        ++_playSpeedIndex;
        PlaySpeed = _playSpeeds[_playSpeedIndex];
        _playTicker.Interval = TimeSpan.FromSeconds(1.0 / PlaySpeed);
        _playTicker.Start();
        UpdateEnable();
      }
    }

    private void HandleUseRankedParameters()
    {
      var toUse = _moduleState.Ranking.Parameters
        .Filter(p => p.IsSelected)
        .Map(p => p.Parameter);

      RequireFalse(toUse.IsEmpty);

      var parameterStates = _moduleState.ParameterStates.Map(
        ps => ps.WithIsSelected(toUse.Contains(ps.Name))
        );

      _moduleState.ParameterStates = parameterStates;
    }

    private void HandleShareRankedParameters()
    {
      var toUse = _moduleState.Ranking.Parameters
        .Filter(p => p.IsSelected)
        .Map(p => p.Parameter);

      RequireFalse(toUse.IsEmpty);

      _moduleState.ParameterStates
        .Filter(ps => toUse.Contains(ps.Name))
        .ShareStates(_appState);
    }

    private void ObserveModuleStateSensitivityDesign(object _)
    {
      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        Populate();
        PopulateRanking();
        UpdateEnable();
      }
    }

    private void ObserveMeasuresStateMorrisOutputMeasures(object _)
    {
      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        Populate();
        UpdateEnable();
      }
    }

    private void ObserveMeasuresStateSelectedOutputName(object _)
    {
      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        Populate();
        UpdateEnable();
      }
    }

    private void ObserveModuleStateRanking(object _)
    {
      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        PopulateRanking();
      }
    }

    private void ObserveIsVisible(object _)
    {
      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        Populate();
        PopulateRanking();
        UpdateEnable();
      }
    }

    private void ObserveSelectedOutputName(object _)
    {
      if (!_reactiveSafeInvoke.React) return;

      var outputName = OutputNames[SelectedOutputName];
      _moduleState.MeasuresState.SelectedOutputName = outputName;
    }

    private void ObserveTraceSelectedX(object _)
    {
      if (!_reactiveSafeInvoke.React) return;

      RequireNotNull(_moduleState.MeasuresState.SelectedOutputName);

      var outputMeasureMap = GetPlotData(_moduleState.MeasuresState.SelectedOutputName);

      if (outputMeasureMap.ContainsKey(_traceViewModel.SelectedX))
      {
        var outputMeasures = outputMeasureMap[_traceViewModel.SelectedX];
        _muStarSigmaViewModel.Plot(outputMeasures.ParameterMeasures);
      }
      else
      {
        _muStarSigmaViewModel.Plot(default);
      }

      UpdateEnable();
    }

    private void Unload()
    {
      _muStarSigmaViewModel.Clear();
      _traceViewModel.Clear();
      OutputNames = default;
      SelectedOutputName = NOT_FOUND;
      _compiledOutputMeasures.Clear();
      RankedParameterViewModels = default;
      RankedUsing = default;
      RankedFrom = default;
      RankedTo = default;
      IsReady = false;
    }

    private void Populate()
    {
      if (!IsVisible)
      {
        Unload();
        return;
      }

      IsReady =
        _moduleState.SensitivityDesign != default &&
        _moduleState.MeasuresState.SelectedOutputName != default &&
        _moduleState.MeasuresState.MorrisOutputMeasures.ContainsKey(
          _moduleState.MeasuresState.SelectedOutputName
        )
        &&
        _moduleState.Trace != default;

      if (!IsReady) return;

      RequireNotNull(_moduleState.Trace);

      var x = _traceViewModel.SelectedX;
      var trace = _moduleState.Trace;
      var independent = _simulation.SimConfig.SimOutput.GetIndependentData(trace);
      if (!independent.Data.Contains(x)) x = independent[0];

      var compiledOutputMeasures = GetPlotData(_moduleState.MeasuresState.SelectedOutputName!);

      var hMin = compiledOutputMeasures.Values.Min(om => om.ParameterMeasures.Min(pm => pm.MuStar));
      var hMax = compiledOutputMeasures.Values.Max(om => om.ParameterMeasures.Max(pm => pm.MuStar));
      var hViewPadding = (hMax - hMin) * 0.05;

      var vMin = compiledOutputMeasures.Values.Min(om => om.ParameterMeasures.Min(pm => pm.Sigma));
      var vMax = compiledOutputMeasures.Values.Max(om => om.ParameterMeasures.Max(pm => pm.Sigma));
      var vViewPadding = (vMax - vMin) * 0.05;

      _muStarSigmaViewModel.SetBounds(
        hMin - hViewPadding,
        hMax + hViewPadding,
        vMin - vViewPadding,
        vMax + vViewPadding
        );

      var hasMeasures = compiledOutputMeasures.TryGetValue(x, out MuStarSigmaOutputMeasures outputMeasures);

      hasMeasures = hasMeasures && !outputMeasures.ParameterMeasures.Exists(
        pm => IsNaN(pm.MuStar) || IsNaN(pm.Sigma)
        );

      if (!hasMeasures)
      {
        var startMeasure = compiledOutputMeasures.FirstOrDefault(
          kvp => kvp.Value.ParameterMeasures.ForAll(
            pm => !IsNaN(pm.MuStar) && !IsNaN(pm.Sigma)
            )
          );

        if (startMeasure.Value.ParameterMeasures.IsEmpty)
        {
          startMeasure = compiledOutputMeasures.Head();
        }

        x = startMeasure.Key;
        outputMeasures = startMeasure.Value;
      }

      _muStarSigmaViewModel.Plot(outputMeasures.ParameterMeasures);

      RequireTrue(independent.Data.Contains(x));

      _traceViewModel.PlotTraceData(
        independent,
        trace[_moduleState.MeasuresState.SelectedOutputName!]
        );

      _traceViewModel.SelectedX = x;

      if (OutputNames.IsEmpty)
      {
        var outputNames = trace.ColumnNames
          .Skip(1)
          .OrderBy(cn => cn.ToUpperInvariant())
          .ToArr();

        OutputNames = outputNames;
      }

      RequireFalse(OutputNames.IsEmpty);

      SelectedOutputName = OutputNames.IndexOf(_moduleState.MeasuresState.SelectedOutputName!);
    }

    private void PopulateRanking()
    {
      RankedParameterViewModels = _moduleState.Ranking.Parameters.Map<IRankedParameterViewModel>(
        p => new RankedParameterViewModel(p.Parameter, p.Score) { IsSelected = p.IsSelected }
        );
      RankedUsing = _moduleState.Ranking.Outputs;
      RankedFrom = _moduleState.Ranking.XBegin;
      RankedTo = _moduleState.Ranking.XEnd;
    }

    private IDictionary<double, MuStarSigmaOutputMeasures> GetPlotData(string outputName)
    {
      if (!_compiledOutputMeasures.ContainsKey(outputName))
      {
        RequireNotNull(_moduleState.Trace);

        var outputMeasures = _moduleState.MeasuresState.MorrisOutputMeasures[outputName];
        var trace = _moduleState.Trace;
        var traceIndependent = _simulation.SimConfig.SimOutput.GetIndependentData(trace);
        var traceDependent = trace[outputName];
        var compiledOutputMeasures = CompileOutputMeasures(
          outputMeasures,
          traceIndependent.Data,
          traceDependent.Data
          );
        _compiledOutputMeasures.Add(outputName, compiledOutputMeasures);
      }

      return _compiledOutputMeasures[outputName];
    }

    private static readonly Arr<int> _playSpeeds = Array(1, 2, 5, 10, 20, 50, 100);

    private readonly IAppState _appState;
    private readonly Simulation _simulation;
    private readonly ModuleState _moduleState;
    private readonly MuStarSigmaViewModel _muStarSigmaViewModel;
    private readonly TraceViewModel _traceViewModel;
    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
    private readonly IDisposable _subscriptions;
    private readonly SortedDictionary<string, IDictionary<double, MuStarSigmaOutputMeasures>> _compiledOutputMeasures =
      new SortedDictionary<string, IDictionary<double, MuStarSigmaOutputMeasures>>();
    private readonly DispatcherTimer _playTicker;
    private int _playSpeedIndex;
    private bool _disposed = false;
  }
}
