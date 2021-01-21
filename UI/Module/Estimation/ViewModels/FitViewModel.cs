using LanguageExt;
using MathNet.Numerics.Statistics;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using ReactiveUI;
using RVis.Base.Extensions;
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

namespace Estimation
{
  internal sealed class FitViewModel : IFitViewModel, INotifyPropertyChanged, IDisposable
  {
    internal FitViewModel(IAppState appState, IAppService appService, IAppSettings appSettings, ModuleState moduleState)
    {
      _appState = appState;
      _moduleState = moduleState;

      _reactiveSafeInvoke = appService.GetReactiveSafeInvoke();

      PlotModel = new PlotModel()
      {
        LegendPlacement = LegendPlacement.Inside,
        LegendPosition = LegendPosition.RightTop,
        LegendOrientation = LegendOrientation.Vertical
      };

      var simulation = _appState.Target.AssertSome();
      var output = simulation.SimConfig.SimOutput;
      var independentVariable = output.IndependentVariable;

      _horizontalAxis = new LinearAxis
      {
        Title = independentVariable.Name,
        Unit = independentVariable.Unit,
        Position = AxisPosition.Bottom
      };
      PlotModel.Axes.Add(_horizontalAxis);

      _verticalAxis = new LinearAxis { Position = AxisPosition.Left };
      PlotModel.Axes.Add(_verticalAxis);

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
           .ObservableForProperty(vm => vm.SelectedOutputName)
           .Subscribe(
              _reactiveSafeInvoke.SuspendAndInvoke<object>(
                ObserveSelectedOutputName
                )
              )

        );

        if (CanConfigureControls) PopulateControls();
        if (CanViewFit) PopulateChart();
      }
    }

    public Arr<IChainViewModel> ChainViewModels
    {
      get => _chainViewModels;
      set => this.RaiseAndSetIfChanged(ref _chainViewModels, value, PropertyChanged);
    }
    private Arr<IChainViewModel> _chainViewModels;

    public Arr<string> OutputNames
    {
      get => _outputNames;
      set => this.RaiseAndSetIfChanged(ref _outputNames, value, PropertyChanged);
    }
    private Arr<string> _outputNames;

    public int SelectedOutputName
    {
      get => _selectedOutputName;
      set => this.RaiseAndSetIfChanged(ref _selectedOutputName, value, PropertyChanged);
    }
    private int _selectedOutputName = NOT_FOUND;

    public PlotModel PlotModel { get; }

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

    private bool CanViewFit =>
      !_moduleState.ChainStates.IsEmpty &&
      _moduleState.ChainStates.ForAll(cs => cs.PosteriorData?.Count > 0) &&
      _moduleState.ChainStates.ForAll(cs => cs.PosteriorData?.Values.All(dt => dt.Rows.Count > 0) == true) &&
      _moduleState.PosteriorState?.BeginIteration < _moduleState.PosteriorState?.EndIteration;

    private void PopulateControls()
    {
      if (_moduleState.EstimationDesign == default)
      {
        _chainViewModelsSubscription?.Dispose();
        _chainViewModelsSubscription = default;
        ChainViewModels = default;
        OutputNames = default;
        SelectedOutputName = NOT_FOUND;
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

      var selectedOutputName = SelectedOutputName.IsFound()
        ? OutputNames[SelectedOutputName]
        : default;

      OutputNames = _moduleState.EstimationDesign.Outputs
        .Map(mo => mo.Name)
        .OrderBy(n => n)
        .ToArr();

      var index = selectedOutputName.IsAString()
        ? OutputNames.IndexOf(selectedOutputName)
        : NOT_FOUND;

      SelectedOutputName = index.IsFound() ? index : 0;
    }

    private void PopulateChart()
    {
      PlotModel.Series.Clear();

      var selectedChainNos = ChainViewModels
        .Filter(vm => vm.IsSelected)
        .Map(vm => vm.No);

      var selectedOutputName = SelectedOutputName.IsFound()
        ? OutputNames[SelectedOutputName]
        : default;

      if (selectedChainNos.IsEmpty || selectedOutputName.IsntAString() || !CanViewFit)
      {
        _verticalAxis.Title = default;

        PlotModel.ResetAllAxes();
        PlotModel.InvalidatePlot(updateData: true);
        return;
      }

      RequireNotNull(_moduleState.EstimationDesign);

      var selectedChainStates = _moduleState.ChainStates.Filter(cs => selectedChainNos.Contains(cs.No));
      var posteriorData = selectedChainStates.Map(cs => cs.PosteriorData![selectedOutputName]);

      var boxModelData = new List<(double X, List<double> Y)>();

      var startPosteriorRow = _moduleState.PosteriorState!.BeginIteration;
      var endPosteriorRow = _moduleState.PosteriorState!.EndIteration;

      posteriorData.Iter(pd =>
      {
        if (boxModelData.IsEmpty())
        {
          var xs = pd.Rows[0].ItemArray.Cast<double>().ToArr();
          xs.Iter(x => boxModelData.Add((x, new List<double>())));
        }

        for (var column = 0; column < pd.Columns.Count; ++column)
        {
          var y = boxModelData[column].Y;

          var row = startPosteriorRow;
          while (row <= endPosteriorRow)
          {
            var datum = pd.Rows[row].Field<double>(column);
            RequireFalse(IsNaN(datum));
            y.Add(datum);

            ++row;
          }
        }
      });

      var boxPlotSeries = new BoxPlotSeries { Title = "Fit" };

      boxModelData.ForEach(t =>
      {
        t.Y.Sort();
        var data = t.Y.ToArray();
        var minimum = SortedArrayStatistics.Minimum(data);
        var lowerQuartile = SortedArrayStatistics.LowerQuartile(data);
        var median = SortedArrayStatistics.Median(data);
        var upperQuartile = SortedArrayStatistics.UpperQuartile(data);
        var maximum = SortedArrayStatistics.Maximum(data);

        boxPlotSeries.Items.Add(new BoxPlotItem(t.X, minimum, lowerQuartile, median, upperQuartile, maximum));
      });

      PlotModel.Series.Add(boxPlotSeries);

      var observations = _moduleState.EstimationDesign.Observations.Filter(o => o.Subject == selectedOutputName);

      observations.Iter(o =>
      {
        var scatterSeries = new ScatterSeries() { Title = o.RefName };

        for (var i = 0; i < o.X.Count; ++i)
        {
          scatterSeries.Points.Add(new ScatterPoint(o.X[i], o.Y[i]));
        }

        PlotModel.Series.Add(scatterSeries);
      });

      var simulation = _appState.Target.AssertSome();
      var output = simulation.SimConfig.SimOutput;
      var element = output.FindElement(selectedOutputName).AssertSome();
      _verticalAxis.Title = element.Name;
      _verticalAxis.Unit = element.Unit;

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
      PopulateChart();
    }

    private void ObserveSelectedOutputName(object _)
    {
      PopulateChart();
    }

    private void ObserveChainViewModelIsSelected(object _)
    {
      PopulateChart();
    }

    private readonly IAppState _appState;
    private readonly ModuleState _moduleState;
    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
    private readonly IDisposable _subscriptions;
    private IDisposable? _chainViewModelsSubscription;
    private readonly LinearAxis _horizontalAxis;
    private readonly LinearAxis _verticalAxis;
    private bool _disposed = false;
  }
}
