using LanguageExt;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
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
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Windows.Input;
using static LanguageExt.Prelude;
using static RVis.Base.Check;
using static RVis.Base.Extensions.NumExt;
using static System.Convert;
using static System.Environment;
using static System.Globalization.CultureInfo;
using static System.Math;
using static System.String;
using IterationUpdateArr = LanguageExt.Arr<(
  int ChainNo,
  LanguageExt.Arr<(
    string Parameter,
    LanguageExt.Arr<(int Iteration, double Value)> Values
    )> Updates
  )>;

namespace Estimation
{
  internal sealed class SimulationViewModel : ISimulationViewModel, INotifyPropertyChanged, IDisposable
  {
    internal SimulationViewModel(IAppState appState, IAppService appService, IAppSettings appSettings, ModuleState moduleState, EstimationDesigns estimationDesigns)
    {
      _appService = appService;
      _moduleState = moduleState;
      _estimationDesigns = estimationDesigns;
      _simulation = appState.Target.AssertSome("No simulation");
      _simData = appState.SimData;
      _estimationDesign = moduleState.EstimationDesign;
      _chainStates = _moduleState.ChainStates;
      _reactiveSafeInvoke = appService.GetReactiveSafeInvoke();

      StartIterating = ReactiveCommand.Create(
        HandleStartIterating,
        this.WhenAny(vm => vm.CanStartIterating, _ => CanStartIterating)
        );

      StopIterating = ReactiveCommand.Create(
        HandleStopIterating,
        this.WhenAny(vm => vm.CanStopIterating, _ => CanStopIterating)
        );

      ShowSettings = ReactiveCommand.Create(
        HandleShowSettings,
        this.WhenAny(vm => vm.CanShowSettings, _ => CanShowSettings)
        );

      SetConvergenceRange = ReactiveCommand.Create(
        HandleSetConvergenceRange,
        this.WhenAny(vm => vm.CanSetConvergenceRange, _ => CanSetConvergenceRange)
        );

      PlotModel = new PlotModel();

      _horizontalAxis = new LinearAxis
      {
        Position = AxisPosition.Bottom,
        AbsoluteMinimum = 1d,
        Minimum = 1d,
        Title = "Iteration"
      };
      PlotModel.Axes.Add(_horizontalAxis);

      _verticalAxis = new LinearAxis
      {
        Position = AxisPosition.Left
      };
      PlotModel.Axes.Add(_verticalAxis);

      _posteriorAnnotation = new RectangleAnnotation
      {
        Fill = OxyColor.FromAColor(120, OxyColors.SkyBlue),
        MinimumX = 0,
        MaximumX = 0
      };
      PlotModel.Annotations.Add(_posteriorAnnotation);

      PlotModel.MouseDown += HandlePlotModelMouseDown;
      PlotModel.MouseMove += HandlePlotModelMouseMove;
      PlotModel.MouseUp += HandlePlotModelMouseUp;

      PlotModel.ApplyThemeToPlotModelAndAxes();

      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        _posteriorBegin = _moduleState.PosteriorState?.BeginIteration;
        _posteriorEnd = _moduleState.PosteriorState?.EndIteration;

        PopulateControls();
        PopulateChartData();
        PopulateChart();
        PopulatePosteriorAnnotation();
        UpdateEnable();
      }

      _subscriptions = new CompositeDisposable(

        appSettings
          .GetWhenPropertyChanged()
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<string?>(ObserveAppSettingsPropertyChange)
            ),

        _moduleState
          .ObservableForProperty(ms => ms.EstimationDesign)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveModuleStateEstimationDesign
              )
            ),

        _moduleState
          .ObservableForProperty(ms => ms.ChainStates)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveModuleStateChainStates
              )
            ),

        _moduleState
          .ObservableForProperty(ms => ms.PosteriorState)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveModuleStatePosteriorState
              )
            ),

        this
          .ObservableForProperty(vm => vm.SelectedParameter)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveSelectedParameter
              )
            ),

        this
          .ObservableForProperty(vm => vm.PosteriorBegin)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObservePosteriorBegin
              )
            ),

        this
          .ObservableForProperty(vm => vm.PosteriorEnd)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObservePosteriorEnd
              )
            )

        );
    }

    public ICommand StartIterating { get; }

    public bool CanStartIterating
    {
      get => _canStartIterating;
      set => this.RaiseAndSetIfChanged(ref _canStartIterating, value, PropertyChanged);
    }
    private bool _canStartIterating;

    public ICommand StopIterating { get; }

    public bool CanStopIterating
    {
      get => _canStopIterating;
      set => this.RaiseAndSetIfChanged(ref _canStopIterating, value, PropertyChanged);
    }
    private bool _canStopIterating;

    public ICommand ShowSettings { get; }

    public bool CanShowSettings
    {
      get => _canShowSettings;
      set => this.RaiseAndSetIfChanged(ref _canShowSettings, value, PropertyChanged);
    }
    private bool _canShowSettings;

    public PlotModel PlotModel { get; }

    public int? PosteriorBegin
    {
      get => _posteriorBegin;
      set => this.RaiseAndSetIfChanged(ref _posteriorBegin, value, PropertyChanged);
    }
    private int? _posteriorBegin;

    public int? PosteriorEnd
    {
      get => _posteriorEnd;
      set => this.RaiseAndSetIfChanged(ref _posteriorEnd, value, PropertyChanged);
    }
    private int? _posteriorEnd;

    public bool CanAdjustConvergenceRange
    {
      get => _canAdjustConvergence;
      set => this.RaiseAndSetIfChanged(ref _canAdjustConvergence, value, PropertyChanged);
    }
    private bool _canAdjustConvergence;

    public ICommand SetConvergenceRange { get; }

    public bool CanSetConvergenceRange
    {
      get => _canSetConvergenceRange;
      set => this.RaiseAndSetIfChanged(ref _canSetConvergenceRange, value, PropertyChanged);
    }
    private bool _canSetConvergenceRange;

    public Arr<string> Parameters
    {
      get => _parameters;
      set => this.RaiseAndSetIfChanged(ref _parameters, value, PropertyChanged);
    }
    private Arr<string> _parameters;

    public int SelectedParameter
    {
      get => _selectedParameter;
      set => this.RaiseAndSetIfChanged(ref _selectedParameter, value, PropertyChanged);
    }
    private int _selectedParameter = NOT_FOUND;

    public bool IsVisible
    {
      get => _isVisible;
      set => this.RaiseAndSetIfChanged(ref _isVisible, value, PropertyChanged);
    }
    private bool _isVisible;

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Dispose() => Dispose(true);

    private void Dispose(bool disposing)
    {
      if (!_disposed)
      {
        if (disposing)
        {
          _subscriptions.Dispose();
          _secondIntervalSubscription?.Dispose();
          _mcmcSim?.Dispose();
        }

        _disposed = true;
      }
    }

    private void HandleStartIterating()
    {
      RequireNotNull(_estimationDesign);
      RequireNull(_mcmcSim);
      RequireNotNull(SynchronizationContext.Current);
      RequireNull(_secondIntervalSubscription);

      try
      {
        _mcmcSim = new McmcSim(_estimationDesign, _simulation, _simData, _chainStates);

        _mcmcSim.ChainsUpdates
          .ObserveOn(SynchronizationContext.Current)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<Arr<(ChainState, Exception?)>>(
              ObserveMcmcSimChainsUpdate
              ),
            _reactiveSafeInvoke.SuspendAndInvoke<Exception>(
              ObserveMcmcSimChainsUpdateError
              )
            );

        _mcmcSim.IterationUpdates.Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<IterationUpdateArr>(
            ObserveMcmcSimIterationUpdate
            )
          );

        _secondIntervalSubscription = _appService.SecondInterval.Subscribe(
          ObserveSecondInterval
          );

        var didStart = _mcmcSim.Iterate();
        RequireTrue(didStart, "Failed to start simulation. Complete.");
      }
      catch (Exception ex)
      {
        _appService.Notify(
          nameof(SimulationViewModel),
          nameof(HandleStartIterating),
          ex
          );
        Logger.Log.Error(ex);
      }

      UpdateEnable();
    }

    private void HandleStopIterating()
    {
      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        _mcmcSim?.StopIterating();
        UpdateEnable();
      }
    }

    private void HandleShowSettings()
    {
      RequireNotNull(_estimationDesign);

      var viewModel = new IterationOptionsViewModel(
        _estimationDesign.TargetAcceptRate,
        _estimationDesign.UseApproximation
        );

      var didOK = _appService.ShowDialog(new IterationOptionsDialog(), viewModel, default);

      if (!didOK) return;

      var doUpdate =
        viewModel.IterationsToAdd.HasValue ||
        viewModel.TargetAcceptRate != _estimationDesign.TargetAcceptRate ||
        viewModel.UseApproximation != _estimationDesign.UseApproximation;

      if (!doUpdate) return;

      var update = new EstimationDesign(
        _estimationDesign.CreatedOn,
        _estimationDesign.Priors,
        _estimationDesign.Outputs,
        _estimationDesign.Observations,
        _estimationDesign.Iterations + (viewModel.IterationsToAdd ?? 0),
        _estimationDesign.BurnIn,
        _estimationDesign.Chains,
        viewModel.TargetAcceptRate,
        viewModel.UseApproximation
        );
      _estimationDesigns.Update(update);
    }

    private void HandleSetConvergenceRange()
    {
      RequireNotNull(PosteriorBegin);
      RequireNotNull(PosteriorEnd);
      RequireNotNull(_estimationDesign);

      _moduleState.PosteriorState = new PosteriorState(PosteriorBegin.Value, PosteriorEnd.Value);

      var pathToEstimationDesign = _estimationDesigns.GetPathToEstimationDesign(_estimationDesign);
      PosteriorState.Save(_moduleState.PosteriorState, pathToEstimationDesign);

      UpdateSetConvergenceRangeEnable();
    }

    private void HandlePlotModelMouseDown(object? sender, OxyMouseDownEventArgs e)
    {
      if (_mcmcSim?.IsIterating == true) return;

      if (e.ChangedButton != OxyMouseButton.Left) return;

      var onXScale = _posteriorAnnotation.InverseTransform(e.Position).X;
      var asIteration = ToInt32(onXScale);

      if (asIteration < 1) return;
      if (asIteration > MaximumPosteriorIteration) return;

      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        PosteriorBegin = asIteration;
        PosteriorEnd = asIteration;
      }

      PopulatePosteriorAnnotation();

      _posteriorAnnotation.Tag = asIteration;

      PlotModel.InvalidatePlot(true);

      e.Handled = true;
    }

    private void HandlePlotModelMouseMove(object? sender, OxyMouseEventArgs e)
    {
      if (_posteriorAnnotation.Tag == default) return;

      var onXScale = _posteriorAnnotation.InverseTransform(e.Position).X;
      var asIteration = ToInt32(onXScale);

      if (asIteration < 1) return;
      if (asIteration > MaximumPosteriorIteration) return;

      var startIteration = RequireInstanceOf<int>(_posteriorAnnotation.Tag);
      var minimumIteration = Min(asIteration, startIteration);
      var maximumIteration = Max(asIteration, startIteration);

      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        PosteriorBegin = minimumIteration;
        PosteriorEnd = maximumIteration;
      }

      PopulatePosteriorAnnotation();

      PlotModel.InvalidatePlot(true);

      e.Handled = true;
    }

    private void HandlePlotModelMouseUp(object? sender, OxyMouseEventArgs e)
    {
      _posteriorAnnotation.Tag = default;
      UpdateSetConvergenceRangeEnable();
    }

    private void ObserveAppSettingsPropertyChange(string? propertyName)
    {
      if (!propertyName.IsThemeProperty()) return;

      PlotModel.ApplyThemeToPlotModelAndAxes();
      PlotModel.InvalidatePlot(false);
    }

    private void ObserveModuleStateEstimationDesign(object _)
    {
      _mcmcSim?.StopIterating();
      _mcmcSim?.Dispose();
      _mcmcSim = default;

      _estimationDesign = _moduleState.EstimationDesign;
      _chainStates = _moduleState.ChainStates;

      PosteriorBegin = _moduleState.PosteriorState?.BeginIteration;
      PosteriorEnd = _moduleState.PosteriorState?.EndIteration;

      PopulateControls();
      PopulateChartData();
      PopulateChart();
      PopulatePosteriorAnnotation();
      UpdateEnable();
    }

    private void ObserveModuleStateChainStates(object _)
    {
      _chainStates = _moduleState.ChainStates;

      PopulateChartData();
      PopulateChart();
      PopulatePosteriorAnnotation();
      UpdateEnable();
    }

    private void ObserveModuleStatePosteriorState(object _)
    {
      PosteriorBegin = _moduleState.PosteriorState?.BeginIteration;
      PosteriorEnd = _moduleState.PosteriorState?.EndIteration;

      PopulatePosteriorAnnotation();

      UpdateEnable();
    }

    private void ObserveSelectedParameter(object _)
    {
      PopulateChart();

      _moduleState.SimulationState.SelectedParameter = Parameters[SelectedParameter];
    }

    private void ObservePosteriorBegin(object _)
    {
      PopulatePosteriorAnnotation();
      PlotModel.InvalidatePlot(updateData: false);
      UpdateSetConvergenceRangeEnable();
    }

    private void ObservePosteriorEnd(object _)
    {
      PopulatePosteriorAnnotation();
      PlotModel.InvalidatePlot(updateData: false);
      UpdateSetConvergenceRangeEnable();
    }

    private void ObserveMcmcSimChainsUpdate(Arr<(ChainState ChainState, Exception? Fault)> update)
    {
      RequireNotNull(_estimationDesign);
      RequireNotNull(_mcmcSim);

      try
      {
        var pathToEstimationDesign = _estimationDesigns.GetPathToEstimationDesign(_estimationDesign);
        ChainState.Save(_mcmcSim.ChainStates, pathToEstimationDesign);
      }
      catch (Exception ex)
      {
        _appService.Notify(
          nameof(SimulationViewModel),
          nameof(ObserveMcmcSimChainsUpdate),
          ex
          );
        Logger.Log.Error(ex);
      }

      var faults = update.Filter(u => u.Fault != default).Map(u => u.Fault!.Message);
      if (!faults.IsEmpty)
      {
        var message = Join(NewLine + NewLine, faults);

        _appService.Notify(
          NotificationType.Error,
          nameof(ObserveMcmcSimChainsUpdate),
          nameof(McmcChain.Fault),
          message
          );
      }

      _chainStates = _mcmcSim.ChainStates;
      _moduleState.ChainStates = _mcmcSim.ChainStates;

      _mcmcSim.Dispose();
      _mcmcSim = default;

      _secondIntervalSubscription?.Dispose();
      _secondIntervalSubscription = default;

      PlotIterationUpdates();

      UpdateEnable();
    }

    private void ObserveMcmcSimChainsUpdateError(Exception ex)
    {
      _mcmcSim?.Dispose();
      _mcmcSim = default;

      _secondIntervalSubscription?.Dispose();
      _secondIntervalSubscription = default;

      _appService.Notify(
        nameof(SimulationViewModel),
        nameof(ObserveMcmcSimChainsUpdateError),
        ex
        );

      PopulateChartData();
      PopulateChart();
      UpdateEnable();
    }

    private void ObserveMcmcSimIterationUpdate(IterationUpdateArr iterationUpdate)
    {
      lock (_syncLock)
      {
        _iterationUpdates.Add(iterationUpdate);
      }
    }

    private void ObserveSecondInterval(long _) =>
      PlotIterationUpdates();

    private void PlotIterationUpdates()
    {
      Arr<IterationUpdateArr> iterationUpdates;

      lock (_syncLock)
      {
        iterationUpdates = _iterationUpdates.ToArr();
        _iterationUpdates.Clear();
      }

      if (iterationUpdates.IsEmpty) return;

      var parameter = Parameters[SelectedParameter];

      iterationUpdates.Iter(iu =>
      {
        iu.Iter(ciu =>
        {
          var maybeParameterValues = ciu.Updates.Find(u => u.Parameter == parameter);
          maybeParameterValues.IfSome(pv =>
          {
            var lineSeries = _series[ciu.ChainNo];
            var dataPoints = pv.Values.Map(v => new DataPoint(v.Iteration, v.Value));
            lineSeries.Points.AddRange(dataPoints);
          });

          ciu.Updates.Iter(u =>
          {
            var chainsData = _chartData[u.Parameter];
            var chainData = chainsData[ciu.ChainNo];
            chainData.AddRange(u.Values);
          });
        });
      });

      PlotModel.InvalidatePlot(updateData: true);
    }

    private void PopulateControls()
    {
      PlotModel.Series.Clear();
      _series.Clear();
      _chartData.Clear();

      if (_estimationDesign == default)
      {
        Parameters = default;
        SelectedParameter = NOT_FOUND;

        _verticalAxis.Title = default;
        PlotModel.InvalidatePlot(updateData: true);

        return;
      }

      Parameters = _estimationDesign.Priors
        .Filter(dp => dp.Distribution.DistributionType != DistributionType.Invariant)
        .Map(dp => dp.Name);

      var index = _moduleState.SimulationState.SelectedParameter.IsAString()
        ? Parameters.IndexOf(_moduleState.SimulationState.SelectedParameter)
        : NOT_FOUND;
      SelectedParameter = index.IsFound() ? index : 0;

      var parameter = _simulation.SimConfig.SimInput.SimParameters.GetParameter(Parameters[SelectedParameter]);
      _verticalAxis.Title = parameter.Name;
      _verticalAxis.Unit = parameter.Unit;

      _horizontalAxis.Maximum = _estimationDesign.Iterations;
      _horizontalAxis.AbsoluteMaximum = _estimationDesign.Iterations;

      var series =
        Range(1, _estimationDesign.Chains)
        .Map(i =>
          new LineSeries
          {
            //MarkerType = MarkerType.Circle,
            //MarkerSize = 2,
            MarkerSize = 0,
            MarkerStrokeThickness = 0,
            InterpolationAlgorithm = null,
            Title = i.ToString(InvariantCulture),
            Tag = i
          })
        .ToArr();

      series.Iter(PlotModel.Series.Add);
      series.Iter(ls => _series.Add((int)ls.Tag, ls));

      var parameterChainDatas = Parameters.Map(p =>
      {
        var parameterChainData = new SortedDictionary<int, List<(int Iteration, double Value)>>();

        var chainsDatas = Range(1, _estimationDesign.Chains).Map(
          no => (no, new List<(int Iteration, double Value)>(_estimationDesign.Iterations))
          );

        parameterChainData.AddRange(chainsDatas);

        return (p, parameterChainData);
      });

      _chartData.AddRange(parameterChainDatas);
    }

    private void PopulateChartData()
    {
      foreach (var chartChains in _chartData.Values)
      {
        foreach (var chainData in chartChains.Values)
        {
          chainData.Clear();
        }
      }

      if (_chainStates.IsEmpty) return;

      RequireTrue(PlotModel.Series.Count == _chainStates.Count);

      var columnNames = _chartData.Keys.ToArr();

      _chainStates.Iter((i, cs) =>
      {
        RequireTrue(columnNames.ForAll(cn => cs.ChainData?.Columns.Contains(cn) == true));

        var chainNo = i + 1;

        RequireTrue(_chartData.Values.ForAll(v => v.ContainsKey(chainNo)));

        var nCompletedIterations = cs.GetCompletedIterations();

        for (var row = 0; row < nCompletedIterations; ++row)
        {
          var dataRow = cs.ChainData!.Rows[row];

          columnNames.Iter(cn =>
          {
            var chartChains = _chartData[cn];
            var chainData = chartChains[chainNo];
            var iteration = row + 1;
            var value = dataRow.Field<double>(cn);
            chainData.Add((iteration, value));
          });
        }
      });
    }

    private void PopulateChart()
    {
      foreach (LineSeries lineSeries in PlotModel.Series)
      {
        lineSeries.Points.Clear();
      }

      if (SelectedParameter == NOT_FOUND || _chartData.IsEmpty())
      {
        PlotModel.InvalidatePlot(updateData: true);
        return;
      }

      var parameterName = Parameters[SelectedParameter];
      RequireTrue(_chartData.ContainsKey(parameterName));

      var chartChains = _chartData[parameterName];

      foreach (var chainNo in _series.Keys)
      {
        RequireTrue(chartChains.ContainsKey(chainNo));
        var lineSeries = _series[chainNo];
        var chainData = chartChains[chainNo];
        var dataPoints = chainData.Map(t => new DataPoint(t.Iteration, t.Value));
        lineSeries.Points.AddRange(dataPoints);
      }

      var parameter = _simulation.SimConfig.SimInput.SimParameters.GetParameter(parameterName);
      _verticalAxis.Title = parameter.Name;
      _verticalAxis.Unit = parameter.Unit;

      PlotModel.ResetAllAxes();

      PlotModel.InvalidatePlot(updateData: true);
    }

    private void PopulatePosteriorAnnotation()
    {
      var minimumIteration = PosteriorBegin ?? 0;
      var maximumIteration = PosteriorEnd ?? 0;

      if (minimumIteration >= maximumIteration)
      {
        minimumIteration = maximumIteration = 0;
      }

      _posteriorAnnotation.MinimumX = minimumIteration;
      _posteriorAnnotation.MaximumX = maximumIteration;
      _posteriorAnnotation.Text = maximumIteration > minimumIteration
        ? $"Convergence: {minimumIteration} - {maximumIteration}"
        : default;
    }

    private void UpdateEnable()
    {
      var isIterating = _mcmcSim?.IsIterating == true;

      if (isIterating)
      {
        CanStartIterating = false;
        CanStopIterating = true;
        CanShowSettings = false;
      }
      else if (_estimationDesign != default)
      {
        var isComplete =
          !_chainStates.IsEmpty &&
          _chainStates.ForAll(cs => cs.GetCompletedIterations() == _estimationDesign.Iterations);
        CanStartIterating = !isComplete;
        CanStopIterating = false;
        CanShowSettings = true;
      }
      else
      {
        CanStartIterating = false;
        CanStopIterating = false;
        CanShowSettings = false;
      }

      UpdateSetConvergenceRangeEnable();
      UpdateAdjustConvergenceRangeEnable();
    }

    private void UpdateSetConvergenceRangeEnable()
    {
      var isIterating = _mcmcSim?.IsIterating == true;

      CanSetConvergenceRange =
        !isIterating &&
        PosteriorBegin < PosteriorEnd &&
        (PosteriorBegin != _moduleState.PosteriorState?.BeginIteration ||
        PosteriorEnd != _moduleState.PosteriorState?.EndIteration);
    }

    private void UpdateAdjustConvergenceRangeEnable()
    {
      var isIterating = _mcmcSim?.IsIterating == true;
      var maximumPosteriorIteration = MaximumPosteriorIteration;

      CanAdjustConvergenceRange =
        !isIterating &&
        maximumPosteriorIteration > _estimationDesign?.BurnIn;
    }

    private int MaximumPosteriorIteration =>
      _chainStates.IsEmpty
        ? 0
        : _chainStates.Min(cs => cs.GetCompletedIterations());

    private readonly IAppService _appService;
    private readonly ModuleState _moduleState;
    private readonly EstimationDesigns _estimationDesigns;
    private readonly Simulation _simulation;
    private readonly ISimData _simData;
    private EstimationDesign? _estimationDesign;
    private Arr<ChainState> _chainStates;
    private McmcSim? _mcmcSim;
    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
    private readonly IDisposable _subscriptions;
    private IDisposable? _secondIntervalSubscription;
    private readonly SortedDictionary<int, LineSeries> _series = new SortedDictionary<int, LineSeries>();
    private readonly LinearAxis _horizontalAxis;
    private readonly LinearAxis _verticalAxis;
    private readonly RectangleAnnotation _posteriorAnnotation;
    private readonly SortedDictionary<string, SortedDictionary<int, List<(int Iteration, double Value)>>> _chartData =
      new SortedDictionary<string, SortedDictionary<int, List<(int, double)>>>();
    private readonly List<IterationUpdateArr> _iterationUpdates = new List<IterationUpdateArr>();
    private readonly object _syncLock = new object();
    private bool _disposed = false;
  }
}
