using LanguageExt;
using MathNet.Numerics.Statistics;
using OxyPlot;
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
using System.Linq;
using System.Reactive.Disposables;
using static LanguageExt.Prelude;
using static RVis.Base.Check;
using static RVis.Base.Extensions.NumExt;
using static System.Double;
using static Estimation.McmcChain;
using OxyPlot.Legends;

namespace Estimation
{
  internal sealed class PosteriorViewModel : IPosteriorViewModel, INotifyPropertyChanged, IDisposable
  {
    internal PosteriorViewModel(IAppState appState, IAppService appService, IAppSettings appSettings, ModuleState moduleState)
    {
      _appState = appState;
      _moduleState = moduleState;

      _reactiveSafeInvoke = appService.GetReactiveSafeInvoke();

      PlotModel = new PlotModel();

      PlotModel.Legends.Add(new Legend
      {
        LegendPlacement = LegendPlacement.Inside,
        LegendPosition = LegendPosition.RightTop,
        LegendOrientation = LegendOrientation.Vertical
      });

      _horizontalAxis = new LinearAxis
      {
        Position = AxisPosition.Bottom
      };
      PlotModel.Axes.Add(_horizontalAxis);

      _leftVerticalAxis = new LinearAxis
      {
        Title = "Frequency",
        Position = AxisPosition.Left,
        Key = nameof(_leftVerticalAxis)
      };
      PlotModel.Axes.Add(_leftVerticalAxis);

      _rightVerticalAxis = new LinearAxis
      {
        Title = "Probability",
        Position = AxisPosition.Right,
        Key = nameof(_rightVerticalAxis)
      };
      PlotModel.Axes.Add(_rightVerticalAxis);

      PlotModel.ApplyThemeToPlotModelAndAxes();

      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        _subscriptions = new CompositeDisposable(

          appSettings
            .GetWhenPropertyChanged()
            .Subscribe(
              _reactiveSafeInvoke.SuspendAndInvoke<string?>(
                ObserveAppSettingsPropertyChange
                )
              ),

          moduleState
            .ObservableForProperty(ms => ms.EstimationDesign)
            .Subscribe(
              _reactiveSafeInvoke.SuspendAndInvoke<object>(
                ObserveModuleStateEstimationDesign
                )
              ),

          moduleState
            .WhenAnyValue(
              ms => ms.ChainStates,
              ms => ms.PosteriorState
              )
            .Subscribe(
              _reactiveSafeInvoke.SuspendAndInvoke<(Arr<ChainState>, PosteriorState?)>(
                ObserveModuleStateEstimationDataChange
                )
              ),

          this
           .ObservableForProperty(vm => vm.SelectedParameterName)
           .Subscribe(
              _reactiveSafeInvoke.SuspendAndInvoke<object>(
                ObserveSelectedParameterName
                )
              )

        );

        if (CanConfigureControls) PopulateControls();
        if (CanViewPosterior) PopulatePosterior();
      }
    }

    public Arr<IChainViewModel> ChainViewModels
    {
      get => _chainViewModels;
      set => this.RaiseAndSetIfChanged(ref _chainViewModels, value, PropertyChanged);
    }
    private Arr<IChainViewModel> _chainViewModels;

    public Arr<string> ParameterNames
    {
      get => _parameterNames;
      set => this.RaiseAndSetIfChanged(ref _parameterNames, value, PropertyChanged);
    }
    private Arr<string> _parameterNames;

    public int SelectedParameterName
    {
      get => _selectedParameterName;
      set => this.RaiseAndSetIfChanged(ref _selectedParameterName, value, PropertyChanged);
    }
    private int _selectedParameterName = NOT_FOUND;

    public PlotModel PlotModel { get; }

    public double? AcceptRate
    {
      get => _acceptRate;
      set => this.RaiseAndSetIfChanged(ref _acceptRate, value, PropertyChanged);
    }
    private double? _acceptRate;

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
          _chainViewModelsSubscription?.Dispose();
        }

        _disposed = true;
      }
    }

    private bool CanConfigureControls =>
      _moduleState.EstimationDesign != default;

    private bool CanViewPosterior =>
      !_moduleState.ChainStates.IsEmpty &&
      _moduleState.ChainStates.ForAll(cs => cs.ChainData?.Rows.Count > 0) &&
      _moduleState.PosteriorState?.BeginIteration < _moduleState.PosteriorState?.EndIteration;

    private void PopulateControls()
    {
      if (_moduleState.EstimationDesign == default)
      {
        _chainViewModelsSubscription?.Dispose();
        _chainViewModelsSubscription = default;
        ChainViewModels = default;
        ParameterNames = default;
        SelectedParameterName = NOT_FOUND;
        return;
      }

      var selectedChains = ChainViewModels.Filter(vm => vm.IsSelected).Map(vm => vm.No);
      ChainViewModels = Range(1, _moduleState.EstimationDesign.Chains)
        .Map(i => new ChainViewModel(i) { IsSelected = selectedChains.IsEmpty || selectedChains.Contains(i) })
        .ToArr<IChainViewModel>();

      var subscriptions = ChainViewModels
        .Map(vm => vm
          .ObservableForProperty(vmm => vmm.IsSelected)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveChainViewModelIsSelected
              )
            )
          )
        .ToArray();
      _chainViewModelsSubscription = new CompositeDisposable(subscriptions);

      var selectedParameterName = SelectedParameterName.IsFound()
        ? ParameterNames[SelectedParameterName]
        : default;

      ParameterNames = _moduleState.EstimationDesign.Priors
        .Filter(mp => mp.Distribution.DistributionType != DistributionType.Invariant)
        .Map(mp => mp.Name)
        .OrderBy(n => n)
        .ToArr();

      var index = selectedParameterName.IsAString() 
        ? ParameterNames.IndexOf(selectedParameterName) 
        : NOT_FOUND;

      SelectedParameterName = index.IsFound() ? index : 0;
    }

    private void PopulatePosterior()
    {
      PlotModel.Series.Clear();

      var selectedChainNos = ChainViewModels
        .Filter(vm => vm.IsSelected)
        .Map(vm => vm.No);

      var selectedParameterName = SelectedParameterName.IsFound()
        ? ParameterNames[SelectedParameterName]
        : default;

      if (selectedChainNos.IsEmpty || selectedParameterName.IsntAString() || !CanViewPosterior)
      {
        _horizontalAxis.Title = default;
        AcceptRate = default;

        PlotModel.ResetAllAxes();
        PlotModel.InvalidatePlot(updateData: true);
        return;
      }

      RequireNotNull(_moduleState.PosteriorState);

      var selectedChainStates = _moduleState.ChainStates.Filter(cs => selectedChainNos.Contains(cs.No));
      var chainData = selectedChainStates.Map(cs => cs.ChainData.AssertNotNull());

      var parameterData = new List<double>();

      var startPosteriorRow = _moduleState.PosteriorState.BeginIteration;
      var endPosteriorRow = _moduleState.PosteriorState.EndIteration;

      var accepts = 0d;

      chainData.Iter(cd =>
      {
        var row = startPosteriorRow;
        while (row <= endPosteriorRow)
        {
          var datum = cd.Rows[row - 1].Field<double>(selectedParameterName);
          RequireFalse(IsNaN(datum));
          parameterData.Add(datum);

          if (row > 1)
          {
            var accept = cd.Rows[row - 1].Field<double>(ToAcceptColumnName(selectedParameterName));
            accepts += accept;
          }

          ++row;
        }
      });

      var (_, nBins) = parameterData.ToArray().FreedmanDiaconis();
      var histogram = new Histogram(parameterData, nBins);

      var histogramSeries = new HistogramSeries
      {
        Title = "Posterior",
        StrokeThickness = 1,
        FillColor = OxyColors.Transparent,
        YAxisKey = nameof(_leftVerticalAxis)
      };

      for (var i = 0; i < histogram.BucketCount; ++i)
      {
        var bucket = histogram[i];
        histogramSeries.Items.Add(
          new HistogramItem(
            bucket.LowerBound,
            bucket.UpperBound,
            bucket.Count / histogram.DataCount,
            (int)bucket.Count
            )
          );
      }

      PlotModel.Series.Add(histogramSeries);

      AcceptRate = accepts / ((endPosteriorRow - startPosteriorRow + 1) * selectedChainNos.Count);

      var prior = _moduleState.PriorStates
        .Find(ps => ps.Name == selectedParameterName)
        .AssertSome();

      var distribution = prior.GetDistribution();

      try
      {
        var (values, densities) = distribution.GetDensities(0.005, 0.995, 100);

        var lineSeries = new LineSeries
        {
          Title = "Prior",
          YAxisKey = nameof(_rightVerticalAxis)
        };

        for (var i = 0; i < values.Count; ++i)
        {
          lineSeries.Points.Add(new DataPoint(values[i], densities[i]));
        }

        PlotModel.Series.Add(lineSeries);
      }
      catch (Exception)
      { /* Not supported by impl */ }

      var simulation = _appState.Target.AssertSome();
      var input = simulation.SimConfig.SimInput;
      var parameter = input.SimParameters.GetParameter(selectedParameterName);
      _horizontalAxis.Title = parameter.Name;
      _horizontalAxis.Unit = parameter.Unit;

      PlotModel.ResetAllAxes();
      PlotModel.InvalidatePlot(updateData: true);
    }

    private void ObserveAppSettingsPropertyChange(string? propertyName)
    {
      if (!propertyName.IsThemeProperty()) return;

      PlotModel.ApplyThemeToPlotModelAndAxes();
      PlotModel.InvalidatePlot(false);
    }

    private void ObserveModuleStateEstimationDesign(object _)
    {
      PopulateControls();
    }

    private void ObserveModuleStateEstimationDataChange((Arr<ChainState>, PosteriorState?) _)
    {
      PopulatePosterior();
    }

    private void ObserveSelectedParameterName(object _)
    {
      PopulatePosterior();
    }

    private void ObserveChainViewModelIsSelected(object _)
    {
      PopulatePosterior();
    }

    private readonly IAppState _appState;
    private readonly ModuleState _moduleState;
    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
    private readonly IDisposable _subscriptions;
    private IDisposable? _chainViewModelsSubscription;
    private readonly LinearAxis _horizontalAxis;
    private readonly LinearAxis _leftVerticalAxis;
    private readonly LinearAxis _rightVerticalAxis;
    private bool _disposed = false;
  }
}
