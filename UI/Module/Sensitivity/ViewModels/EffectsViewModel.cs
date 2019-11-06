using LanguageExt;
using ReactiveUI;
using RVis.Data.Extensions;
using RVis.Model;
using RVisUI.Model;
using RVisUI.Model.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using static LanguageExt.Prelude;
using static RVis.Base.Check;
using static RVis.Base.Extensions.NumExt;
using static Sensitivity.LowryPlotData;
using static Sensitivity.MeasuresOps;
using static System.Double;
using DataTable = System.Data.DataTable;

namespace Sensitivity
{
  internal sealed class EffectsViewModel : ViewModelBase, IEffectsViewModel
  {
    internal EffectsViewModel(
      IAppState appState,
      IAppService appService,
      IAppSettings appSettings,
      ModuleState moduleState,
      SensitivityDesigns sensitivityDesigns
      )
    {
      _appState = appState;
      _appService = appService;
      _moduleState = moduleState;
      _sensitivityDesigns = sensitivityDesigns;

      _lowryViewModel = new LowryViewModel(appService, appSettings, moduleState.LowryState);
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

      _reactiveSafeInvoke = appService.GetReactiveSafeInvoke();

      _subscriptions = new CompositeDisposable(

        moduleState.ObservableForProperty(ms => ms.SensitivityDesign).Subscribe(
          ObserveModuleStateSensitivityDesign
          ),

        moduleState.MeasuresState.ObservableForProperty(ms => ms.OutputMeasures).Subscribe(
          ObserveMeasuresStateOutputMeasures
          ),

        moduleState.MeasuresState.ObservableForProperty(ms => ms.SelectedOutputName).Subscribe(
          ObserveMeasuresStateSelectedOutputName
          ),

        this.ObservableForProperty(vm => vm.SelectedOutputName).Subscribe(
          ObserveSelectedOutputName
          ),

        _traceViewModel.ObservableForProperty(vm => vm.SelectedX).Subscribe(
          ObserveTraceSelectedX
          )

        );

      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        Populate();
      }
    }

    public bool IsVisible
    {
      get => _isVisible;
      set => this.RaiseAndSetIfChanged(ref _isVisible, value);
    }
    private bool _isVisible;

    public ILowryViewModel LowryViewModel => _lowryViewModel;

    public ITraceViewModel TraceViewModel => _traceViewModel;

    public Arr<string> OutputNames
    {
      get => _outputNames;
      set => this.RaiseAndSetIfChanged(ref _outputNames, value);
    }
    private Arr<string> _outputNames;

    public bool CanSelectOutputName
    {
      get => _canSelectOutputName;
      set => this.RaiseAndSetIfChanged(ref _canSelectOutputName, value);
    }
    private bool _canSelectOutputName;

    public int SelectedOutputName
    {
      get => _selectedOutputName;
      set => this.RaiseAndSetIfChanged(ref _selectedOutputName, value);
    }
    private int _selectedOutputName = NOT_FOUND;

    public int PlaySpeed
    {
      get => _playSpeed;
      set => this.RaiseAndSetIfChanged(ref _playSpeed, value);
    }
    private int _playSpeed;

    public ICommand PlaySimulation { get; }

    public bool CanPlaySimulation
    {
      get => _canPlaySimulation;
      set => this.RaiseAndSetIfChanged(ref _canPlaySimulation, value);
    }
    private bool _canPlaySimulation;

    public ICommand StopSimulation { get; }

    public bool CanStopSimulation
    {
      get => _canStopSimulation;
      set => this.RaiseAndSetIfChanged(ref _canStopSimulation, value);
    }
    private bool _canStopSimulation;

    public ICommand PlaySlower { get; }

    public bool CanPlaySlower
    {
      get => _canPlaySlower;
      set => this.RaiseAndSetIfChanged(ref _canPlaySlower, value);
    }
    private bool _canPlaySlower;

    public ICommand PlayFaster { get; }

    public bool CanPlayFaster
    {
      get => _canPlayFaster;
      set => this.RaiseAndSetIfChanged(ref _canPlayFaster, value);
    }
    private bool _canPlayFaster;

    public override void HandleCancelTask()
    {
      _cancellationTokenSource?.Cancel();
    }

    protected override void Dispose(bool disposing)
    {
      if (!_disposed)
      {
        if (disposing)
        {
          _subscriptions.Dispose();
          _traceViewModel.Dispose();
          _lowryViewModel.Dispose();

          if (_cancellationTokenSource?.IsCancellationRequested == false)
          {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
          }
        }

        _disposed = true;
      }

      base.Dispose(disposing);
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

    private void HandlePlayTick(object sender, EventArgs e)
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

    private void ObserveModuleStateSensitivityDesign(object _)
    {
      if (_moduleState.SensitivityDesign == default)
      {
        using (_reactiveSafeInvoke.SuspendedReactivity)
        {
          _cancellationTokenSource?.Cancel();
          _lowryViewModel.Clear();
          _traceViewModel.Clear();
          OutputNames = default;
          SelectedOutputName = NOT_FOUND;
          _compiledOutputMeasures.Clear();

          IsVisible = false;
        }
      }
    }

    private void ObserveMeasuresStateOutputMeasures(object _)
    {
      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        Populate();
      }
    }

    private void ObserveMeasuresStateSelectedOutputName(object _)
    {
      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        Populate();
      }
    }

    private async Task GenerateOutputMeasuresAsync(string outputName, ServerLicense serverLicense)
    {
      using (serverLicense)
      {
        _cancellationTokenSource = new CancellationTokenSource();

        TaskName = "Generate Output Measures";
        IsRunningTask = true;
        CanCancelTask = true;

        try
        {
          var measures = await Task.Run(
            () => GenerateOutputMeasures(
              outputName,
              _moduleState.SensitivityDesign.SerializedDesign,
              _moduleState.SensitivityDesign.Samples,
              _moduleState.DesignOutputs,
              serverLicense.Client,
              _cancellationTokenSource.Token,
              s => _appService.ScheduleLowPriorityAction(() => RaiseTaskMessageEvent(s))
            ),
            _cancellationTokenSource.Token
            );

          _moduleState.MeasuresState.SelectedOutputName = outputName;

          _moduleState.MeasuresState.OutputMeasures =
            _moduleState.MeasuresState.OutputMeasures.Add(outputName, measures);

          _sensitivityDesigns.SaveOutputMeasures(
            _moduleState.SensitivityDesign,
            outputName,
            measures.FirstOrder,
            measures.TotalOrder,
            measures.Variance
            );

          var nansInFirstOrder = measures.FirstOrder.Rows
            .Cast<DataRow>()
            .Skip(1)
            .Any(dr => dr.ItemArray.OfType<double>().Any(IsNaN));

          var nansInTotalOrder = measures.TotalOrder.Rows
            .Cast<DataRow>()
            .Skip(1)
            .Any(dr => dr.ItemArray.OfType<double>().Any(IsNaN));

          if (nansInFirstOrder || nansInTotalOrder)
          {
            _appState.Status = "NaN(s) generated by sensitivity::tell()";
          }
        }
        catch (OperationCanceledException)
        {
          // expected
        }
        catch (Exception ex)
        {
          _appService.Notify(
            nameof(EffectsViewModel),
            nameof(GenerateOutputMeasuresAsync),
            ex
            );
        }

        IsRunningTask = false;
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = default;
      }
    }

    private void ObserveSelectedOutputName(object _)
    {
      if (!_reactiveSafeInvoke.React) return;

      var outputName = OutputNames[SelectedOutputName];

      if (_moduleState.MeasuresState.OutputMeasures.ContainsKey(outputName))
      {
        _moduleState.MeasuresState.SelectedOutputName = outputName;
        return;
      }

      var loadedMeasures = _sensitivityDesigns.LoadOutputMeasures(
        _moduleState.SensitivityDesign,
        outputName,
        out (DataTable FirstOrder, DataTable TotalOrder, DataTable Variance) measures
        );

      if (loadedMeasures)
      {
        _moduleState.MeasuresState.OutputMeasures =
          _moduleState.MeasuresState.OutputMeasures.Add(outputName, measures);
        _moduleState.MeasuresState.SelectedOutputName = outputName;
        return;
      }

      if (_moduleState.DesignOutputs.IsEmpty)
      {
        _appService.Notify(
          NotificationType.Information,
          "Change Selected Output",
          outputName,
          "Cannot compute measures as design outputs are not loaded. Reload data from design screen to continue."
          );
        _moduleState.MeasuresState.SelectedOutputName = outputName;
        return;
      }

      void SomeServer(ServerLicense serverLicense)
      {
        var __ = GenerateOutputMeasuresAsync(outputName, serverLicense);
      }

      void NoServer()
      {
        _appService.Notify(
          NotificationType.Information,
          nameof(VarianceViewModel),
          nameof(ObserveSelectedOutputName),
          "No R server available."
          );
      }

      _appService.RVisServerPool.RequestServer().Match(SomeServer, NoServer);
    }

    private void ObserveTraceSelectedX(object _)
    {
      if (!_reactiveSafeInvoke.React) return;

      var outputMeasureMap = GetPlotData(_moduleState.MeasuresState.SelectedOutputName);

      if (outputMeasureMap.ContainsKey(_traceViewModel.SelectedX))
      {
        var outputMeasures = outputMeasureMap[_traceViewModel.SelectedX];
        _lowryViewModel.PlotParameterData(outputMeasures.ParameterMeasures);
      }
      else
      {
        _lowryViewModel.PlotParameterData(default);
      }

      UpdateEnable();
    }

    private void Populate()
    {
      if (_moduleState.SensitivityDesign == default || _moduleState.MeasuresState.OutputMeasures.IsEmpty)
      {
        IsVisible = false;
        return;
      }

      var canPlot = _moduleState.MeasuresState.OutputMeasures.ContainsKey(
        _moduleState.MeasuresState.SelectedOutputName
        );

      canPlot = canPlot && _moduleState.Trace != default;

      if (!canPlot)
      {
        _lowryViewModel.Clear();
        _traceViewModel.Clear();
        return;
      }

      var compiledOutputMeasures = GetPlotData(_moduleState.MeasuresState.SelectedOutputName);

      var x = _traceViewModel.SelectedX;
      var trace = _moduleState.Trace;
      var independent = trace.GetIndependentVariable();
      if (!independent.Data.Contains(x)) x = independent[0];

      var hasMeasures = compiledOutputMeasures.TryGetValue(x, out OutputMeasures outputMeasures);

      hasMeasures = hasMeasures && !outputMeasures.ParameterMeasures.Exists(
        pm => IsNaN(pm.MainEffect) || IsNaN(pm.Interaction)
        );

      if (!hasMeasures)
      {
        var startMeasure = compiledOutputMeasures.FirstOrDefault(
          kvp => kvp.Value.ParameterMeasures.ForAll(
            pm => !IsNaN(pm.MainEffect) && !IsNaN(pm.Interaction)
            )
          );

        if (startMeasure.Value == default)
        {
          startMeasure = compiledOutputMeasures.Head();
        }

        x = startMeasure.Key;
        outputMeasures = startMeasure.Value;
      }

      _lowryViewModel.PlotParameterData(outputMeasures.ParameterMeasures);

      RequireTrue(independent.Data.Contains(x));

      _traceViewModel.PlotTraceData(
        independent,
        trace[_moduleState.MeasuresState.SelectedOutputName]
        );

      if (OutputNames.IsEmpty)
      {
        var outputNames = trace.ColumnNames
          .Skip(1)
          .OrderBy(cn => cn.ToUpperInvariant())
          .ToArr();

        OutputNames = outputNames;
      }

      RequireFalse(OutputNames.IsEmpty);

      _traceViewModel.SelectedX = x;
      SelectedOutputName = OutputNames.IndexOf(_moduleState.MeasuresState.SelectedOutputName);
      UpdateEnable();

      if (!IsVisible)
      {
        IsVisible = true;
      }
    }

    private IDictionary<double, OutputMeasures> GetPlotData(string outputName)
    {
      if (!_compiledOutputMeasures.ContainsKey(outputName))
      {
        var outputMeasures = _moduleState.MeasuresState.OutputMeasures[outputName];
        var trace = _moduleState.Trace;
        var traceIndependent = trace.GetIndependentVariable();
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
    private readonly IAppService _appService;
    private readonly ModuleState _moduleState;
    private readonly SensitivityDesigns _sensitivityDesigns;
    private readonly LowryViewModel _lowryViewModel;
    private readonly TraceViewModel _traceViewModel;
    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
    private readonly IDisposable _subscriptions;
    private CancellationTokenSource _cancellationTokenSource;
    private readonly SortedDictionary<string, IDictionary<double, OutputMeasures>> _compiledOutputMeasures =
      new SortedDictionary<string, IDictionary<double, OutputMeasures>>();
    private readonly DispatcherTimer _playTicker;
    private int _playSpeedIndex;
    private bool _disposed = false;
  }
}
