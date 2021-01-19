using LanguageExt;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using ReactiveUI;
using RVis.Base.Extensions;
using RVis.Data;
using RVisUI.AppInf.Design;
using RVisUI.AppInf.Extensions;
using RVisUI.Model;
using RVisUI.Model.Extensions;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reflection;
using System.Windows.Input;
using static LanguageExt.Prelude;
using static RVis.Base.Check;
using static RVisUI.Wpf.WpfTools;
using static Sensitivity.ChartOptionsViewModel;
using static System.Double;
using static System.Math;

namespace Sensitivity
{
  internal sealed class TraceViewModel : ITraceViewModel, INotifyPropertyChanged, IDisposable
  {
    internal TraceViewModel() 
      : this(new AppService(), new AppSettings(), new TraceState())
    {
      RequireTrue(IsInDesignMode);
      _traceSeries.IsVisible = true;
    }

    internal TraceViewModel(IAppService appService, IAppSettings appSettings, TraceState traceState)
    {
      _appService = appService;
      _appSettings = appSettings;
      _traceState = traceState;

      _viewHeight = traceState.ViewHeight ?? 200.0;

      PlotModel = new PlotModel
      {
        Title = _traceState.ChartTitle,
        IsLegendVisible = false
      };
#pragma warning disable CS0618 // Type or member is obsolete
      PlotModel.MouseDown += HandleTracePlotModelMouseDown;
#pragma warning restore CS0618 // Type or member is obsolete

      _traceHorizontalAxis = new LinearAxis
      {
        Position = AxisPosition.Bottom,
        MinimumPadding = 0,
        MaximumPadding = 0.06,
        AbsoluteMinimum = _traceState.HorizontalAxisAbsoluteMinimum,
        Minimum = _traceState.HorizontalAxisMinimum,
        AbsoluteMaximum = _traceState.HorizontalAxisAbsoluteMaximum,
        Maximum = _traceState.HorizontalAxisMaximum
      };
      PlotModel.Axes.Add(_traceHorizontalAxis);

      _traceVerticalAxis = new LinearAxis
      {
        Position = AxisPosition.Left,
        MinimumPadding = 0,
        MaximumPadding = 0.06,
        AbsoluteMinimum = _traceState.VerticalAxisAbsoluteMinimum,
        Minimum = _traceState.VerticalAxisMinimum,
        AbsoluteMaximum = _traceState.VerticalAxisAbsoluteMaximum,
        Maximum = _traceState.VerticalAxisMaximum,
        Key = "output"
      };
      PlotModel.Axes.Add(_traceVerticalAxis);

      _traceSeries = new LineSeries
      {
        MarkerType = MarkerType.Circle,
        MarkerFill = _traceState.MarkerFill ?? OxyColors.DodgerBlue,
        Color = _traceState.SeriesColor ?? OxyColors.DodgerBlue,
        InterpolationAlgorithm = InterpolationAlgorithms.CatmullRomSpline
      };
#pragma warning disable CS0618 // Type or member is obsolete
      _traceSeries.MouseDown += HandleTraceMouseDown;
#pragma warning restore CS0618 // Type or member is obsolete

      PlotModel.Series.Add(_traceSeries);

      _verticalCursor = new LineAnnotation { Type = LineAnnotationType.Vertical };
#pragma warning disable CS0618 // Type or member is obsolete
      _verticalCursor.MouseDown += HandleVerticalCursorMouseDown;
      _verticalCursor.MouseMove += HandleVerticalCursorMouseMove;
      _verticalCursor.MouseUp += HandleVerticalCursorMouseUp;
#pragma warning restore CS0618 // Type or member is obsolete
      PlotModel.Annotations.Add(_verticalCursor);

      PlotModel.ApplyThemeToPlotModelAndAxes();

      ResetAxes = ReactiveCommand.Create(HandleResetAxes);
      ShowOptions = ReactiveCommand.Create(HandleShowOptions);

      _reactiveSafeInvoke = appService.GetReactiveSafeInvoke();

      _subscriptions = new CompositeDisposable(

        _appSettings
          .GetWhenPropertyChanged()
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<string?>(ObserveAppSettingsPropertyChange)
            ),

        this
          .ObservableForProperty(vm => vm.ViewHeight)
          .Subscribe(_reactiveSafeInvoke.SuspendAndInvoke<object>(ObserveViewHeight)),

        this
          .ObservableForProperty(vm => vm.SelectedX)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveSelectedX
              )
            )

        );
    }

    public PlotModel PlotModel { get; }

    public Arr<double> XValues
    {
      get => _xValues;
      set => this.RaiseAndSetIfChanged(ref _xValues, value, PropertyChanged);
    }
    private Arr<double> _xValues;

    public Arr<double> YValues
    {
      get => _yValues;
      set => this.RaiseAndSetIfChanged(ref _yValues, value, PropertyChanged);
    }
    private Arr<double> _yValues;

    public double SelectedX
    {
      get => _selectedX;
      set => this.RaiseAndSetIfChanged(ref _selectedX, value, PropertyChanged);
    }
    private double _selectedX;

    public ICommand ResetAxes { get; }
    public ICommand ShowOptions { get; }

    public double ViewHeight
    {
      get => _viewHeight;
      set => this.RaiseAndSetIfChanged(ref _viewHeight, value, PropertyChanged);
    }
    private double _viewHeight = 200d;

    public event PropertyChangedEventHandler? PropertyChanged;

    internal void PlotTraceData(NumDataColumn independent, NumDataColumn dependent)
    {
      var nPoints = dependent.Data.Count;

      RequireTrue(nPoints == independent.Data.Count);

      _traceSeries.Points.Clear();

      for (var i = 0; i < nPoints; ++i)
      {
        var x = independent.Data[i];
        var y = dependent.Data[i];

        if (IsNaN(x) || IsNaN(y)) continue;

        _traceSeries.Points.Add(new DataPoint(x, y));
      }

      XValues = _traceSeries.Points.Select(p => p.X).ToArr();
      YValues = _traceSeries.Points.Select(p => p.Y).ToArr();

      _traceHorizontalAxis.Title = independent.Name;
      _traceVerticalAxis.Title = dependent.Name;

      PlotModel.ResetAllAxes();
      PlotModel.InvalidatePlot(true);
    }

    internal void Clear()
    {
      _traceSeries.Points.Clear();
      _traceHorizontalAxis.Title = default;
      _traceVerticalAxis.Title = default;
      XValues = default;
      YValues = default;
      _horizontalResetMinimum = NaN;
      _horizontalResetMaximum = NaN;
      _verticalResetMinimum = NaN;
      _verticalResetMaximum = NaN;
    }

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

    private void HandleResetAxes()
    {
      PlotModel.ResetAllAxes();
      PlotModel.InvalidatePlot(false);
    }

    private void HandleShowOptions() =>
      ShowOptionsDialog(CHART_PAGE);

    private void ObserveAppSettingsPropertyChange(string? propertyName)
    {
      if (!propertyName.IsThemeProperty()) return;

      PlotModel.ApplyThemeToPlotModelAndAxes();
      PlotModel.InvalidatePlot(false);
    }

    private void ObserveViewHeight(object _) =>
      _traceState.ViewHeight = ViewHeight;

    private void ObserveSelectedX(object _)
    {
      RequireTrue(XValues.IsEmpty || XValues.Contains(SelectedX));
      _verticalCursor.X = SelectedX;
      PlotModel.InvalidatePlot(false);
    }

    private void HandleTracePlotModelMouseDown(object? sender, OxyMouseDownEventArgs e)
    {
      if (e.ChangedButton != OxyMouseButton.Left) return;

      var dataPoint = _traceSeries.InverseTransform(e.Position);
      var minimumVisibleOutput = _traceVerticalAxis.ActualMinimum;
      var maximumVisibleOutput = _traceVerticalAxis.ActualMaximum;
      var minimumVisibleTime = _traceHorizontalAxis.ActualMinimum;
      var maximumVisibleTime = _traceHorizontalAxis.ActualMaximum;

      if (dataPoint.X < minimumVisibleTime)
      {
        var actualTitleFontSizeProperty = _traceVerticalAxis
          .GetType()
          .GetProperty("ActualTitleFontSize", BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Instance)
          .AssertNotNull();
        var titleFontSize = (double)actualTitleFontSizeProperty.GetValue(_traceVerticalAxis).AssertNotNull();
        var titleEndsAt = PlotModel.Padding.Left + titleFontSize + _traceVerticalAxis.AxisTickToLabelDistance;
        var isLabelClick = e.Position.X < titleEndsAt;

        ShowOptionsDialog(isLabelClick ? LABELS_PAGE : AXES_PAGE);
      }
      else if (dataPoint.Y < minimumVisibleOutput)
      {
        var actualTitleFontSizeProperty = _traceHorizontalAxis
          .GetType()
          .GetProperty("ActualTitleFontSize", BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Instance)
          .AssertNotNull();
        var titleFontSize = (double)actualTitleFontSizeProperty.GetValue(_traceHorizontalAxis).AssertNotNull();
        var titleEndsAt = PlotModel.Padding.Left + titleFontSize + _traceHorizontalAxis.AxisTickToLabelDistance;
        var distanceOver = PlotModel.PlotAndAxisArea.Bottom - e.Position.Y;
        var isLabelClick = distanceOver < titleEndsAt;

        ShowOptionsDialog(isLabelClick ? LABELS_PAGE : AXES_PAGE);
      }
      else if (dataPoint.Y > maximumVisibleOutput || dataPoint.X > maximumVisibleTime)
      {
        ShowOptionsDialog(LABELS_PAGE);
      }
      else
      {
        using (_reactiveSafeInvoke.SuspendedReactivity)
        {
          SelectedX = GetNearestXValue(dataPoint.X);
        }

        _verticalCursor.X = SelectedX;
        PlotModel.InvalidatePlot(false);
      }

      e.Handled = true;
    }

    private void HandleTraceMouseDown(object? sender, OxyMouseDownEventArgs e)
    {
      if (!_reactiveSafeInvoke.React) return;

      var trackerHitResult = _traceSeries.GetNearestPoint(e.Position, false);
      RequireTrue(XValues.Contains(trackerHitResult.DataPoint.X));

      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        SelectedX = trackerHitResult.DataPoint.X;
      }

      _verticalCursor.X = SelectedX;
      PlotModel.InvalidatePlot(false);

      e.Handled = true;
    }

    private void HandleVerticalCursorMouseDown(object? sender, OxyMouseDownEventArgs e)
    {
      if (e.ChangedButton != OxyMouseButton.Left) return;

      _verticalCursor.StrokeThickness *= 2;
      PlotModel.InvalidatePlot(false);

      e.Handled = true;
    }

    private void HandleVerticalCursorMouseMove(object? sender, OxyMouseEventArgs e)
    {
      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        SelectedX = GetNearestXValue(_verticalCursor.X);
      }

      _verticalCursor.X = _verticalCursor.InverseTransform(e.Position).X;
      PlotModel.InvalidatePlot(false);

      e.Handled = true;
    }

    private void HandleVerticalCursorMouseUp(object? sender, OxyMouseEventArgs e)
    {
      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        SelectedX = GetNearestXValue(_verticalCursor.X);
      }

      _verticalCursor.X = SelectedX;
      _verticalCursor.StrokeThickness /= 2;
      PlotModel.InvalidatePlot(false);

      e.Handled = true;
    }

    private double GetNearestXValue(double x) =>
      XValues
        .Map(v => new { value = v, distance = Abs(v - x) })
        .OrderBy(p => p.distance)
        .First().value;

    private void ShowOptionsDialog(int page)
    {
      var markerColor = _traceSeries.MarkerFill;
      var markerColorIndex = OxyColorData.OxyColors.FindIndex(ocd => ocd.OxyColor == markerColor);

      var lineColor = _traceSeries.Color;
      var lineColorIndex = OxyColorData.OxyColors.FindIndex(ocd => ocd.OxyColor == lineColor);

      var viewModel = new ChartOptionsViewModel(_appService)
      {
        Page = page,
        ChartTitle = PlotModel.Title,
        YAxisTitle = _traceVerticalAxis.Title,
        XAxisTitle = _traceHorizontalAxis.Title,
        ElementNames = _elementNames,
        ElementColorIndices = new[] { markerColorIndex, lineColorIndex },
        WindowTitle = "Trace Plot Options",
        ShowAxesTab = true,

        HorizontalAxisMinimumAuto = IsNaN(_traceHorizontalAxis.Minimum),
        HorizontalAxisMinimum = _traceHorizontalAxis.ActualMinimum,
        HorizontalAxisMaximumAuto = IsNaN(_traceHorizontalAxis.Maximum),
        HorizontalAxisMaximum = _traceHorizontalAxis.ActualMaximum,

        HorizontalAxisAbsoluteMinimumAuto = _traceHorizontalAxis.AbsoluteMinimum == MinValue,
        HorizontalAxisAbsoluteMinimum = _traceHorizontalAxis.AbsoluteMinimum == MinValue
          ? _traceHorizontalAxis.ActualMinimum
          : _traceHorizontalAxis.AbsoluteMinimum,
        HorizontalAxisAbsoluteMaximumAuto = _traceHorizontalAxis.AbsoluteMaximum == MaxValue,
        HorizontalAxisAbsoluteMaximum = _traceHorizontalAxis.AbsoluteMaximum == MaxValue
          ? _traceHorizontalAxis.ActualMaximum
          : _traceHorizontalAxis.ActualMaximum,

        VerticalAxisMinimumAuto = IsNaN(_traceVerticalAxis.Minimum),
        VerticalAxisMinimum = _traceVerticalAxis.ActualMinimum,
        VerticalAxisMaximumAuto = IsNaN(_traceVerticalAxis.Maximum),
        VerticalAxisMaximum = _traceVerticalAxis.ActualMaximum,

        VerticalAxisAbsoluteMinimumAuto = _traceVerticalAxis.AbsoluteMinimum == MinValue,
        VerticalAxisAbsoluteMinimum = _traceVerticalAxis.AbsoluteMinimum == MinValue
          ? _traceVerticalAxis.ActualMinimum
          : _traceVerticalAxis.AbsoluteMinimum,
        VerticalAxisAbsoluteMaximumAuto = _traceVerticalAxis.AbsoluteMaximum == MaxValue,
        VerticalAxisAbsoluteMaximum = _traceVerticalAxis.AbsoluteMaximum == MaxValue
          ? _traceVerticalAxis.ActualMaximum
          : _traceVerticalAxis.ActualMaximum,
      };

      var ok = _appService.ShowDialog(new ChartOptionsDialog(), viewModel, default);

      if (ok)
      {
        if (IsNaN(_horizontalResetMinimum))
        {
          _horizontalResetMinimum = _traceHorizontalAxis.ActualMinimum;
          _horizontalResetMaximum = _traceHorizontalAxis.ActualMaximum;
          _verticalResetMinimum = _traceVerticalAxis.ActualMinimum;
          _verticalResetMaximum = _traceVerticalAxis.ActualMaximum;
        }

        PlotModel.Title = viewModel.ChartTitle;
        _traceVerticalAxis.Title = viewModel.YAxisTitle;
        _traceHorizontalAxis.Title = viewModel.XAxisTitle;
        _traceSeries.MarkerFill = OxyColorData.OxyColors[viewModel.ElementColorIndices[MARKER_ELEMENT]].OxyColor;
        _traceSeries.Color = OxyColorData.OxyColors[viewModel.ElementColorIndices[LINE_ELEMENT]].OxyColor;
        _traceHorizontalAxis.AbsoluteMinimum = viewModel.HorizontalAxisAbsoluteMinimumAuto
          ? MinValue
          : viewModel.HorizontalAxisAbsoluteMinimum;
        _traceHorizontalAxis.AbsoluteMaximum = viewModel.HorizontalAxisAbsoluteMaximumAuto
          ? MaxValue
          : viewModel.HorizontalAxisAbsoluteMaximum;

        double minimum;
        if (viewModel.HorizontalAxisMinimumAuto)
        {
          minimum = _horizontalResetMinimum;
          _traceHorizontalAxis.Minimum = NaN;
        }
        else
        {
          minimum = viewModel.HorizontalAxisMinimum;
          _traceHorizontalAxis.Minimum = minimum;
        }

        double maximum;
        if (viewModel.HorizontalAxisMaximumAuto)
        {
          maximum = _horizontalResetMaximum;
          _traceHorizontalAxis.Maximum = NaN;
        }
        else
        {
          maximum = viewModel.HorizontalAxisMaximum;
          _traceHorizontalAxis.Maximum = maximum;
        }

        _traceHorizontalAxis.Zoom(minimum, maximum);

        _traceVerticalAxis.AbsoluteMinimum = viewModel.VerticalAxisAbsoluteMinimumAuto
          ? MinValue
          : viewModel.VerticalAxisAbsoluteMinimum;
        _traceVerticalAxis.AbsoluteMaximum = viewModel.VerticalAxisAbsoluteMaximumAuto
          ? MaxValue
          : viewModel.VerticalAxisAbsoluteMaximum;
        minimum = viewModel.VerticalAxisMinimumAuto
          ? _traceVerticalAxis.ActualMinimum
          : viewModel.VerticalAxisMinimum;
        maximum = viewModel.VerticalAxisMaximumAuto
          ? _traceVerticalAxis.ActualMaximum
          : viewModel.VerticalAxisMaximum;

        if (viewModel.VerticalAxisMinimumAuto)
        {
          minimum = _verticalResetMinimum;
          _traceVerticalAxis.Minimum = NaN;
        }
        else
        {
          minimum = viewModel.VerticalAxisMinimum;
          _traceVerticalAxis.Minimum = minimum;
        }

        if (viewModel.VerticalAxisMaximumAuto)
        {
          maximum = _verticalResetMaximum;
          _traceVerticalAxis.Maximum = NaN;
        }
        else
        {
          maximum = viewModel.VerticalAxisMaximum;
          _traceVerticalAxis.Maximum = maximum;
        }

        _traceVerticalAxis.Zoom(minimum, maximum);

        PlotModel.InvalidatePlot(false);

        _traceState.ChartTitle = PlotModel.Title;
        _traceState.XAxisTitle = _traceHorizontalAxis.Title;
        _traceState.YAxisTitle = _traceVerticalAxis.Title;

        _traceState.MarkerFill = _traceSeries.MarkerFill;
        _traceState.SeriesColor = _traceSeries.Color;

        _traceState.HorizontalAxisMinimum = _traceHorizontalAxis.Minimum;
        _traceState.HorizontalAxisMaximum = _traceHorizontalAxis.Maximum;
        _traceState.HorizontalAxisAbsoluteMinimum = _traceHorizontalAxis.AbsoluteMinimum;
        _traceState.HorizontalAxisAbsoluteMaximum = _traceHorizontalAxis.AbsoluteMaximum;

        _traceState.VerticalAxisMinimum = _traceVerticalAxis.Minimum;
        _traceState.VerticalAxisMaximum = _traceVerticalAxis.Maximum;
        _traceState.VerticalAxisAbsoluteMinimum = _traceVerticalAxis.AbsoluteMinimum;
        _traceState.VerticalAxisAbsoluteMaximum = _traceVerticalAxis.AbsoluteMaximum;
      }
    }

    private readonly Arr<string> _elementNames = Array("Marker", "Line");
    private const int MARKER_ELEMENT = 0;
    private const int LINE_ELEMENT = 1;

    private readonly IAppService _appService;
    private readonly IAppSettings _appSettings;
    private readonly TraceState _traceState;
    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
    private readonly IDisposable _subscriptions;
    private readonly LineSeries _traceSeries;
    private readonly LineAnnotation _verticalCursor;
    private readonly LinearAxis _traceHorizontalAxis;
    private readonly LinearAxis _traceVerticalAxis;
    private double _horizontalResetMinimum = NaN;
    private double _horizontalResetMaximum = NaN;
    private double _verticalResetMinimum = NaN;
    private double _verticalResetMaximum = NaN;
    private bool _disposed = false;
  }
}
