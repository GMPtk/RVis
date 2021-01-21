using LanguageExt;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using ReactiveUI;
using RVis.Base.Extensions;
using RVis.Data;
using RVis.Model;
using RVis.Model.Extensions;
using RVisUI.AppInf.Extensions;
using RVisUI.Model;
using RVisUI.Model.Extensions;
using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Windows.Input;
using static RVis.Base.Extensions.NumExt;

namespace Plot
{
  internal class TraceDataPlotViewModel : ReactiveObject, ITraceDataPlotViewModel, IDisposable
  {
    public TraceDataPlotViewModel(
      IAppState appState,
      IAppService appService,
      IAppSettings appSettings,
      TraceDataPlotState traceDataPlotState
      )
    {
      _appState = appState;
      _appSettings = appSettings;
      _simulation = _appState.Target.AssertSome("No simulation");
      State = traceDataPlotState;

      _depVarConfigViewModel = new DepVarConfigViewModel(
        _simulation.SimConfig.SimOutput,
        _appState.SimEvidence,
        appService,
        traceDataPlotState.DepVarConfigState
        );

      _isSeriesTypeLine = traceDataPlotState.IsSeriesTypeLine;
      _isAxesOriginLockedToZeroZero = traceDataPlotState.IsAxesOriginLockedToZeroZero;

      _xAbsoluteMinimum = _isAxesOriginLockedToZeroZero ? 0.0 : double.MinValue;
      _xMinimum = traceDataPlotState.XMinimum ?? (_isAxesOriginLockedToZeroZero ? 0.0 : double.NaN);
      _xMaximum = traceDataPlotState.XMaximum ?? double.NaN;

      _yAbsoluteMinimum = _isAxesOriginLockedToZeroZero ? 0.0 : double.MinValue;
      _yMinimum = traceDataPlotState.YMinimum ?? (_isAxesOriginLockedToZeroZero ? 0.0 : double.NaN);
      _yMaximum = traceDataPlotState.YMaximum ?? double.NaN;

      ToggleSeriesType = ReactiveCommand.Create(HandleToggleSeriesType);

      ToggleLockAxesOriginToZeroZero = ReactiveCommand.Create(HandleToggleIsAxesOriginLockedToZeroZero);

      ResetAxisRanges = ReactiveCommand.Create(HandleResetAxisRanges);

      RemoveChart = ReactiveCommand.Create(HandleRemoveChart);

      _reactiveSafeInvoke = appService.GetReactiveSafeInvoke();

      _plotModel = new PlotModel
      {
        Background = OxyColors.Transparent,
        LegendPosition = LegendPosition.BottomRight
      };

      _traceHorizontalAxis = new LinearAxis
      {
        Position = AxisPosition.Bottom,
        MinimumPadding = 0,
        MaximumPadding = 0.06,
      };
      _traceHorizontalAxis.AxisChanged += (s, e) => { if (_reactiveSafeInvoke.React) HandleAxisChanged(); };
      _plotModel.Axes.Add(_traceHorizontalAxis);

      _traceVerticalAxis = new LinearAxis
      {
        Position = AxisPosition.Left,
        MinimumPadding = 0,
        MaximumPadding = 0.06,
        Key = nameof(_traceVerticalAxis),
      };
      _traceVerticalAxis.AxisChanged += (s, e) => { if (_reactiveSafeInvoke.React) HandleAxisChanged(); };
      _traceInsetAxis = new LinearAxis
      {
        Position = AxisPosition.Right,
        MinimumPadding = 0,
        MaximumPadding = 0,
        TickStyle = TickStyle.Inside,
        LabelFormatter = x => null,
        StartPosition = 0.4,
        EndPosition = 0.6,
        Key = nameof(_traceInsetAxis),
      };

      _traceLogVerticalAxis = new LogarithmicAxis
      {
        Position = AxisPosition.Left,
        MinimumPadding = 0,
        MaximumPadding = 0.06,
        Key = nameof(_traceLogVerticalAxis)
      };
      _traceLogVerticalAxis.AxisChanged += (s, e) => { if (_reactiveSafeInvoke.React) HandleAxisChanged(); };
      _traceLogInsetAxis = new LogarithmicAxis
      {
        Position = AxisPosition.Right,
        MinimumPadding = 0,
        MaximumPadding = 0.06,
        TickStyle = TickStyle.Inside,
        LabelFormatter = x => null,
        StartPosition = 0.4,
        EndPosition = 0.6,
        Key = nameof(_traceLogInsetAxis)
      };

      ApplyTheme();

      _subscriptions = new CompositeDisposable(

        _appSettings
          .GetWhenPropertyChanged()
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<string?>(ObserveAppSettingsPropertyChange)
            ),

        traceDataPlotState
          .ObservableForProperty(x => x.IsVisible)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(ObserveTraceDataPlotStateIsVisible)
            ),

        traceDataPlotState.DepVarConfigState
          .GetWhenPropertyChanged()
          .Subscribe(
            ObserveDepVarConfigStatePropertyChanged
            ),

        this
          .ObservableForProperty(vm => vm.DataSet)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(ObserveDataSet)
          )

      );
    }

    public TraceDataPlotState State { get; }

    public Arr<(string SeriesName, Arr<(string SerieName, NumDataTable Serie)> Series)> DataSet
    {
      get => _dataSet;
      set => this.RaiseAndSetIfChanged(ref _dataSet, value);
    }
    private Arr<(string SeriesName, Arr<(string SerieName, NumDataTable Serie)> Series)> _dataSet;

    public Arr<string> SeriesNames
    {
      get => _seriesNames;
      set => this.RaiseAndSetIfChanged(ref _seriesNames, value);
    }
    private Arr<string> _seriesNames;

    public int SelectedIndexSeries
    {
      get => _selectedIndexSeries;
      set => this.RaiseAndSetIfChanged(ref _selectedIndexSeries, value);
    }
    private int _selectedIndexSeries = NOT_FOUND;

    public IDepVarConfigViewModel DepVarConfigViewModel => _depVarConfigViewModel;

    public PlotModel PlotModel
    {
      get => _plotModel;
      set => this.RaiseAndSetIfChanged(ref _plotModel, value);
    }
    private PlotModel _plotModel;

    public ICommand ToggleSeriesType { get; }

    public bool IsSeriesTypeLine
    {
      get => _isSeriesTypeLine;
      set => this.RaiseAndSetIfChanged(ref _isSeriesTypeLine, value);
    }
    private bool _isSeriesTypeLine;

    public ICommand ToggleLockAxesOriginToZeroZero { get; }

    public bool IsAxesOriginLockedToZeroZero
    {
      get => _isAxesOriginLockedToZeroZero;
      set => this.RaiseAndSetIfChanged(ref _isAxesOriginLockedToZeroZero, value);
    }
    private bool _isAxesOriginLockedToZeroZero;

    public ICommand ResetAxisRanges { get; }

    public ICommand RemoveChart { get; }

    public void Dispose() => Dispose(true);

    private void ObserveDataSet(object _)
    {
      if (_dataSet.IsEmpty)
      {
        SeriesNames = default;
        SelectedIndexSeries = NOT_FOUND;
      }
      else
      {
        var seriesNames = _dataSet.Map(t => t.SeriesName);

        var selectedIndexSeries = State.SelectedSeriesName.IsAString()
          ? seriesNames.IndexOf(State.SelectedSeriesName)
          : NOT_FOUND;
        if (selectedIndexSeries.IsntFound()) selectedIndexSeries = 0;
        SeriesNames = seriesNames;
        SelectedIndexSeries = selectedIndexSeries;
        State.SelectedSeriesName = seriesNames[selectedIndexSeries];
      }

      if (State.IsVisible)
      {
        PlotModel.Series.Clear();
        ConfigurePlotModel();
        PlotDepVar();
        PlotInset();
        PlotSupplementaryTraces();
        PlotObservations();
        ConfigureLegend();
        PlotModel.InvalidatePlot(updateData: true);
      }
    }

    private void HandleToggleSeriesType()
    {
      State.IsSeriesTypeLine = !IsSeriesTypeLine;
      IsSeriesTypeLine = State.IsSeriesTypeLine;

      PlotModel.Series.Clear();
      PlotDepVar();
      PlotInset();
      PlotSupplementaryTraces();
      PlotObservations();
      ConfigureLegend();
      PlotModel.InvalidatePlot(updateData: true);
    }

    private void HandleToggleIsAxesOriginLockedToZeroZero()
    {
      State.IsAxesOriginLockedToZeroZero = !IsAxesOriginLockedToZeroZero;
      IsAxesOriginLockedToZeroZero = State.IsAxesOriginLockedToZeroZero;

      if (IsAxesOriginLockedToZeroZero)
      {
        State.XMinimum = null;
        State.YMinimum = null;
      }

      _xAbsoluteMinimum = IsAxesOriginLockedToZeroZero ? 0.0 : double.MinValue;
      _xMinimum = State.XMinimum ?? (IsAxesOriginLockedToZeroZero ? 0.0 : double.NaN);

      _yAbsoluteMinimum = IsAxesOriginLockedToZeroZero ? 0.0 : double.MinValue;
      _yMinimum = State.YMinimum ?? (IsAxesOriginLockedToZeroZero ? 0.0 : double.NaN);

      _traceHorizontalAxis.AbsoluteMinimum = _xAbsoluteMinimum;
      _traceVerticalAxis.AbsoluteMinimum = _yAbsoluteMinimum;

      if (IsAxesOriginLockedToZeroZero) PlotModel.InvalidatePlot(false);
    }

    private void HandleAxisChanged()
    {
      var verticalAxis = PlotModel.Axes
        .Find(a => a.Position == AxisPosition.Left)
        .AssertSome("Missing vertical axis");

      State.XMaximum = _traceHorizontalAxis.ActualMaximum;
      State.YMaximum = verticalAxis.ActualMaximum;

      if (!IsAxesOriginLockedToZeroZero || _traceHorizontalAxis.ActualMinimum > 0.0)
      {
        State.XMinimum = _traceHorizontalAxis.ActualMinimum;
      }

      if (!IsAxesOriginLockedToZeroZero || verticalAxis.ActualMinimum > 0.0)
      {
        State.YMinimum = verticalAxis.ActualMinimum;
      }

      _xMinimum = State.XMinimum ?? (IsAxesOriginLockedToZeroZero ? 0.0 : double.NaN);
      _xMaximum = State.XMaximum ?? double.NaN;

      _yMinimum = State.YMinimum ?? (IsAxesOriginLockedToZeroZero ? 0.0 : double.NaN);
      _yMaximum = State.YMaximum ?? double.NaN;
    }

    private void HandleResetAxisRanges()
    {
      State.XMaximum = null;
      State.YMaximum = null;
      State.XMinimum = null;
      State.YMinimum = null;

      _xMinimum = State.XMinimum ?? (IsAxesOriginLockedToZeroZero ? 0.0 : double.NaN);
      _xMaximum = State.XMaximum ?? double.NaN;

      _yMinimum = State.YMinimum ?? (IsAxesOriginLockedToZeroZero ? 0.0 : double.NaN);
      _yMaximum = State.YMaximum ?? double.NaN;

      _traceHorizontalAxis.AbsoluteMinimum = _xAbsoluteMinimum;
      _traceHorizontalAxis.Minimum = _xMinimum;
      _traceHorizontalAxis.Maximum = _xMaximum;

      _traceVerticalAxis.AbsoluteMinimum = _yAbsoluteMinimum;
      _traceVerticalAxis.Minimum = _yMinimum;
      _traceVerticalAxis.Maximum = _yMaximum;

      _traceLogVerticalAxis.AbsoluteMinimum = _yAbsoluteMinimum;
      _traceLogVerticalAxis.Minimum = _yMinimum;
      _traceLogVerticalAxis.Maximum = _yMaximum;

      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        PlotModel.ResetAllAxes();
      }

      PlotModel.InvalidatePlot(false);
    }

    private void HandleRemoveChart()
    {
      State.IsVisible = false;
    }

    private void ObserveAppSettingsPropertyChange(string? propertyName)
    {
      if (PlotModel is null) return;
      if (!propertyName.IsThemeProperty()) return;

      ApplyTheme();
      PlotModel.InvalidatePlot(updateData: true);
    }

    private void ObserveTraceDataPlotStateIsVisible(object _)
    {
      if (!State.IsVisible) return;

      PlotModel.Series.Clear();
      ConfigurePlotModel();
      PlotDepVar();
      PlotInset();
      PlotSupplementaryTraces();
      PlotObservations();
      ConfigureLegend();
      PlotModel.InvalidatePlot(updateData: true);
    }

    private void ObserveDepVarConfigStatePropertyChanged(string? propertyName)
    {
      if (!State.IsVisible) return;

      switch (propertyName)
      {
        case nameof(DepVarConfigState.SelectedElementName):
        case nameof(DepVarConfigState.IsScaleLogarithmic):
          PlotModel.Series.Clear();
          ConfigurePlotModel();
          PlotDepVar();
          PlotInset();
          PlotSupplementaryTraces();
          PlotObservations();
          ConfigureLegend();
          PlotModel.InvalidatePlot(updateData: true);
          break;

        case nameof(DepVarConfigState.SelectedInsetElementName):
          PlotInset();
          PlotModel.InvalidatePlot(updateData: true);
          break;

        case nameof(DepVarConfigState.SupplementaryElementNames):
          PlotSupplementaryTraces();
          ConfigureLegend();
          PlotModel.InvalidatePlot(updateData: true);
          break;

        case nameof(DepVarConfigState.ObservationsReferences):
          PlotObservations();
          ConfigureLegend();
          PlotModel.InvalidatePlot(updateData: true);
          break;
      }
    }

    private void ApplyTheme()
    {
      _plotModel.ApplyThemeToPlotModelAndAxes();

      if (SelectedIndexSeries.IsFound())
      {
        var dataSet = DataSet.LookUp(SeriesNames[SelectedIndexSeries]).AssertSome();
        _plotModel.DefaultColors = GetPalette(dataSet[0].Serie.NColumns - 1).Colors;
      }
    }

    private void ConfigurePlotModel()
    {
      var series = SelectedIndexSeries.IsFound()
        ? DataSet.LookUp(SeriesNames[SelectedIndexSeries])
        : default;

      series.IfSome(s =>
      {
        var ivElement = _simulation.SimConfig.SimOutput.IndependentVariable;
        _traceHorizontalAxis.Title = ivElement.Name;
        _traceHorizontalAxis.Unit = ivElement.Unit;

        _plotModel.DefaultColors = GetPalette(s[0].Serie.NColumns - 1).Colors;
      });

      _plotModel.Axes.Clear();

      _traceHorizontalAxis.AbsoluteMinimum = _xAbsoluteMinimum;
      _traceHorizontalAxis.Minimum = _xMinimum;
      _traceHorizontalAxis.Maximum = _xMaximum;
      _plotModel.Axes.Add(_traceHorizontalAxis);

      var verticalAxis = State.DepVarConfigState.IsScaleLogarithmic ? _traceLogVerticalAxis as Axis : _traceVerticalAxis;
      verticalAxis.AbsoluteMinimum = _yAbsoluteMinimum;
      verticalAxis.Minimum = _yMinimum;
      verticalAxis.Maximum = _yMaximum;
      _plotModel.Axes.Add(verticalAxis);

      var insetAxis = State.DepVarConfigState.IsScaleLogarithmic ? _traceLogInsetAxis as Axis : _traceInsetAxis;
      _plotModel.Axes.Add(insetAxis);

      _traceHorizontalAxis.AbsoluteMinimum = IsAxesOriginLockedToZeroZero ? 0.0 : double.MinValue;
      verticalAxis.AbsoluteMinimum = IsAxesOriginLockedToZeroZero ? 0.0 : double.MinValue;
    }

    private enum SerieType
    {
      DepVar,
      Inset,
      Supplementary,
      Observations
    }

    private static void AddSerie(
      (SerieType SerieType, string ID) serieID,
      string serieTitle,
      NumDataColumn independentVariable,
      NumDataColumn dependentVariable,
      LineStyle lineStyle,
      PlotModel plotModel
      )
    {
      var verticalAxis = plotModel.Axes
        .Find(a => a.Position == AxisPosition.Left)
        .AssertSome("Missing vertical axis");

      Series series;

      if (lineStyle == LineStyle.None)
      {
        var scatterSeries = new ScatterSeries
        {
          YAxisKey = verticalAxis.Key,
          Title = serieTitle,
          MarkerType = MarkerType.Plus,
          MarkerStroke = plotModel.DefaultColors[plotModel.Series.Count % plotModel.DefaultColors.Count],
          MarkerSize = 1
        };

        for (var i = 0; i < independentVariable.Length; ++i)
        {
          scatterSeries.Points.Add(new ScatterPoint(independentVariable[i], dependentVariable[i]));
        }

        series = scatterSeries;
      }
      else
      {
        var interpolatePoints = independentVariable.Length < INTERPOLATION_LIMIT;

        var lineSeries = new LineSeries
        {
          YAxisKey = verticalAxis.Key,
          Title = serieTitle,
          LineStyle = lineStyle,
          InterpolationAlgorithm = interpolatePoints ? InterpolationAlgorithms.CatmullRomSpline : default
        };

        for (var i = 0; i < independentVariable.Length; ++i)
        {
          lineSeries.Points.Add(new DataPoint(independentVariable[i], dependentVariable[i]));
        }

        series = lineSeries;
      }

      series.Tag = serieID;
      series.Title = serieTitle;
      plotModel.Series.Add(series);
    }

    private void PlotDepVar()
    {
      if (State.DepVarConfigState.SelectedElementName.IsAString())
      {
        var series = DataSet.LookUp(SeriesNames[SelectedIndexSeries]).AssertSome();

        var verticalAxis = PlotModel.Axes
          .Find(a => a.Position == AxisPosition.Left)
          .AssertSome("Missing vertical axis");

        var nSerie = series.Count;
        var (serieName, serie) = series[0];

        var independentVariable = _simulation.SimConfig.SimOutput.GetIndependentData(serie);
        var dependentVariable = serie[State.DepVarConfigState.SelectedElementName];
        AddSerie(
          (SerieType.DepVar, dependentVariable.Name),
          nSerie > 1 ? serieName : dependentVariable.Name,
          independentVariable,
          dependentVariable,
          IsSeriesTypeLine ? LineStyle.Solid : LineStyle.None,
          PlotModel
          );
        verticalAxis.Title = dependentVariable.Name;
        var dvElement = _simulation.SimConfig.SimOutput
          .FindElement(dependentVariable.Name)
          .AssertSome();
        verticalAxis.Unit = dvElement.Unit;

        for (var i = 1; i < nSerie; ++i)
        {
          (serieName, serie) = series[i];
          independentVariable = _simulation.SimConfig.SimOutput.GetIndependentData(serie);
          dependentVariable = serie[State.DepVarConfigState.SelectedElementName];
          AddSerie(
            (SerieType.DepVar, dependentVariable.Name),
            serieName,
            independentVariable,
            dependentVariable,
            IsSeriesTypeLine ? LineStyle.Solid : LineStyle.None,
            PlotModel
            );
        }
      }
    }

    private void PlotInset()
    {
      var existingSeries = PlotModel.Series
        .Where(s =>
        {
          var (serieType, _) = (ValueTuple<SerieType, string>)s.Tag;
          return serieType == SerieType.Inset;
        })
        .ToArr();

      existingSeries.Iter(s => PlotModel.Series.Remove(s));

      var insetAxis = PlotModel.Axes
        .Find(a => a.Position == AxisPosition.Right)
        .AssertSome("Missing inset axis");

      if (State.DepVarConfigState.SelectedInsetElementName.IsntAString())
      {
        insetAxis.IsAxisVisible = false;
        return;
      }

      insetAxis.IsAxisVisible = true;

      var series = DataSet.LookUp(SeriesNames[SelectedIndexSeries]).AssertSome();
      var (_, serie) = series[0];
      var independentVariable = _simulation.SimConfig.SimOutput.GetIndependentData(serie);
      var dependentVariable = serie[State.DepVarConfigState.SelectedInsetElementName];

      var color = OxyColors.LightGray;
      color = OxyColor.FromArgb(100, color.R, color.G, color.B);

      if (IsSeriesTypeLine)
      {
        var interpolatePoints = independentVariable.Length < INTERPOLATION_LIMIT;

        var lineSeries = new LineSeries
        {
          YAxisKey = insetAxis.Key,
          LineStyle = LineStyle.Solid,
          Color = color,
          InterpolationAlgorithm = interpolatePoints ? InterpolationAlgorithms.CatmullRomSpline : default,
          Tag = (SerieType.Inset, State.DepVarConfigState.SelectedInsetElementName)
        };

        for (var i = 0; i < independentVariable.Length; ++i)
        {
          lineSeries.Points.Add(new DataPoint(independentVariable[i], dependentVariable[i]));
        }

        PlotModel.Series.Add(lineSeries);
      }
      else
      {
        var scatterSeries = new ScatterSeries
        {
          YAxisKey = insetAxis.Key,
          MarkerType = MarkerType.Plus,
          MarkerStroke = color,
          MarkerSize = 1,
          Tag = (SerieType.Inset, State.DepVarConfigState.SelectedInsetElementName)
        };

        for (var i = 0; i < independentVariable.Length; ++i)
        {
          scatterSeries.Points.Add(new ScatterPoint(independentVariable[i], dependentVariable[i]));
        }

        PlotModel.Series.Add(scatterSeries);
      }
    }

    private void ConfigureLegend() =>
      PlotModel.IsLegendVisible = PlotModel.Series.Count > 1;

    private void PlotObservations()
    {
      var observationsSeries = PlotModel.Series
        .Where(s =>
        {
          var (serieType, _) = (ValueTuple<SerieType, string>)s.Tag;
          return serieType == SerieType.Observations;
        })
        .ToArr();

      observationsSeries.Iter(s => PlotModel.Series.Remove(s));

      var verticalAxis = PlotModel.Axes
        .Find(a => a.Position == AxisPosition.Left)
        .AssertSome("Missing vertical axis");

      var seriesIndexOffset = PlotModel.Series.Count;

      State.DepVarConfigState.ObservationsReferences.Iter((i, r) =>
      {
        var maybeObservations = _appState.SimEvidence.GetObservations(r);
        maybeObservations.IfSome(o =>
        {
          if (o.Subject != State.DepVarConfigState.SelectedElementName) return;

          PlotModel.AddScatterSeries(
            i + seriesIndexOffset,
            o.RefName,
            o.X,
            o.Y,
            _plotModel.DefaultColors,
            verticalAxis.Key,
            (SerieType.Observations, r)
            );
        });
      });
    }

    private void PlotSupplementaryTraces()
    {
      var series = PlotModel.Series
        .Select(s =>
        {
          var (serieType, id) = (ValueTuple<SerieType, string>)s.Tag;
          return new { SerieType = serieType, ID = id, Series = s };
        })
        .Where(s => s.SerieType == SerieType.Supplementary)
        .ToArr();

      series
        .Filter(s => !State.DepVarConfigState.SupplementaryElementNames.Contains(s.ID))
        .Iter(s => PlotModel.Series.Remove(s.Series));

      State.DepVarConfigState.SupplementaryElementNames
        .Filter(n => !series.Exists(s => s.ID == n))
        .Iter(PlotSupplementaryTrace);
    }

    private void PlotSupplementaryTrace(string name)
    {
      var dataSet = DataSet.LookUp(SeriesNames[SelectedIndexSeries]).AssertSome();
      var useIndex = dataSet.Count > 1;
      for (var i = 0; i < dataSet.Count; ++i)
      {
        var serieTitle = useIndex ? $"{name} [{(1 + i):00}]" : name;
        var independentVariable = _simulation.SimConfig.SimOutput.GetIndependentData(dataSet[i].Serie);
        var dependentVariable = dataSet[i].Serie[name];
        AddSerie(
          (SerieType.Supplementary, name),
          serieTitle,
          independentVariable,
          dependentVariable,
          IsSeriesTypeLine ? LineStyle.Solid : LineStyle.None,
          PlotModel
          );
      }
    }

    private void Dispose(bool disposing)
    {
      if (!_disposed)
      {
        if (disposing)
        {
          _depVarConfigViewModel.Dispose();
          _subscriptions.Dispose();
        }

        _disposed = true;
      }
    }

    private OxyPalette GetPalette(int numberOfColors) =>
      _appSettings.IsBaseDark
        ? OxyPalettes.Cool(numberOfColors)
        : OxyPalettes.Rainbow(numberOfColors);

    private const int INTERPOLATION_LIMIT = 30;

    private readonly IAppState _appState;
    private readonly IAppSettings _appSettings;
    private readonly Simulation _simulation;
    private readonly DepVarConfigViewModel _depVarConfigViewModel;
    private readonly IDisposable _subscriptions;
    private readonly LinearAxis _traceHorizontalAxis;
    private readonly LinearAxis _traceVerticalAxis;
    private readonly LinearAxis _traceInsetAxis;
    private readonly LogarithmicAxis _traceLogVerticalAxis;
    private readonly LogarithmicAxis _traceLogInsetAxis;
    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;

    private double _xAbsoluteMinimum;
    private double _xMinimum;
    private double _xMaximum;
    private double _yAbsoluteMinimum;
    private double _yMinimum;
    private double _yMaximum;

    private bool _disposed = false;
  }
}
