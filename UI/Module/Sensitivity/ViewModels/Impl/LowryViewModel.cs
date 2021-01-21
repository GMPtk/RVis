using LanguageExt;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using ReactiveUI;
using RVis.Base.Extensions;
using RVisUI.AppInf;
using RVisUI.AppInf.Design;
using RVisUI.AppInf.Extensions;
using RVisUI.Model;
using RVisUI.Model.Extensions;
using System;
using System.ComponentModel;
using System.IO;
using System.Reactive.Disposables;
using System.Windows.Input;
using static LanguageExt.Prelude;
using static RVis.Base.Check;
using static RVisUI.Wpf.WpfTools;
using static Sensitivity.ChartOptionsViewModel;
using static System.Convert;
using static System.IO.Path;

namespace Sensitivity
{
  internal sealed class LowryViewModel : ILowryViewModel, INotifyPropertyChanged, IDisposable
  {

    internal LowryViewModel() : this(new AppService(), new AppSettings(), new LowryState())
    {
      RequireTrue(IsInDesignMode);
    }

    internal LowryViewModel(IAppService appService, IAppSettings appSettings, LowryState lowryState)
    {
      _appService = appService;
      _appSettings = appSettings;
      _lowryState = lowryState;

      PlotModel = new PlotModel
      {
        Title = _lowryState.ChartTitle,
        IsLegendVisible = true,
        LegendPosition = LegendPosition.BottomRight
      };

      PlotModel.MouseDown += HandlePlotModelMouseDown;

      _lowryStackAxis = new CategoryAxis
      {
        Position = AxisPosition.Bottom,
        Key = nameof(_lowryStackAxis),
        Title = _lowryState.XAxisTitle,
        Angle = -35,
        IsZoomEnabled = false,
        IsPanEnabled = false
      };
      PlotModel.Axes.Add(_lowryStackAxis);

      _lowrySmokeAxis = new LinearAxis
      {
        Position = AxisPosition.Bottom,
        MinimumPadding = 0,
        MaximumPadding = 0.06,
        AbsoluteMinimum = 0,
        Minimum = 0,
        Key = nameof(_lowrySmokeAxis),
        IsAxisVisible = false,
        IsZoomEnabled = false,
        IsPanEnabled = false
      };
      PlotModel.Axes.Add(_lowrySmokeAxis);

      _lowryVerticalAxis = new LinearAxis
      {
        Position = AxisPosition.Left,
        MinimumPadding = 0,
        MaximumPadding = 0.06,
        AbsoluteMinimum = 0,
        AbsoluteMaximum = 1.0,
        Minimum = 0.0,
        Maximum = 1.0,
        Title = _lowryState.YAxisTitle,
        IsZoomEnabled = false,
        IsPanEnabled = false
      };
      PlotModel.Axes.Add(_lowryVerticalAxis);

      _mainEffectsSeries = new ColumnSeries
      {
        Title = "Main Effects",
        IsStacked = true,
        StrokeColor = OxyColors.Black,
        StrokeThickness = 1,
        FillColor = _lowryState.MainEffectsFillColor ?? OxyColors.ForestGreen,
        XAxisKey = nameof(_lowryStackAxis)
      };
      _mainEffectsSeries.MouseDown += HandleMainEffectsSeriesMouseDown;

      PlotModel.Series.Add(_mainEffectsSeries);

      _interactionsSeries = new ColumnSeries
      {
        Title = "Interactions",
        IsStacked = true,
        StrokeColor = OxyColors.Black,
        StrokeThickness = 1,
        FillColor = _lowryState.InteractionsFillColor ?? OxyColors.DarkGoldenrod,
        XAxisKey = nameof(_lowryStackAxis)
      };
      _interactionsSeries.MouseDown += HandleInteractionsSeriesMouseDown;

      PlotModel.Series.Add(_interactionsSeries);

      _smokeSeries = new AreaSeries
      {
        DataFieldX2 = "IndependentVar",
        DataFieldY2 = "Minimum",
        Fill = _lowryState.SmokeFill ?? OxyColors.LightBlue,
        Color = OxyColors.Red,
        MarkerFill = OxyColors.Transparent,
        StrokeThickness = 0,
        DataFieldX = "IndependentVar",
        DataFieldY = "Maximum",
        Title = "Maximum/Minimum",
        XAxisKey = nameof(_lowrySmokeAxis),
        RenderInLegend = false
      };
      PlotModel.Series.Add(_smokeSeries);

      PlotModel.ApplyThemeToPlotModelAndAxes();

      UpdateSize = ReactiveCommand.Create(HandleUpdateSize);
      ResetAxes = ReactiveCommand.Create(HandleResetAxes);
      ShowOptions = ReactiveCommand.Create(HandleShowOptions);
      ExportImage = ReactiveCommand.Create(HandleExportImage);

      _reactiveSafeInvoke = appService.GetReactiveSafeInvoke();

      _subscriptions = new CompositeDisposable(

        _appSettings
          .GetWhenPropertyChanged()
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<string?>(
              ObserveAppSettingsPropertyChange
              )
            )

        );
    }

    public PlotModel PlotModel { get; }

    public ICommand UpdateSize { get; }

    public int Width
    {
      get => _width;
      set => this.RaiseAndSetIfChanged(ref _width, value, PropertyChanged);
    }
    private int _width;

    public int Height
    {
      get => _height;
      set => this.RaiseAndSetIfChanged(ref _height, value, PropertyChanged);
    }
    private int _height;

    public ICommand ResetAxes { get; }
    public ICommand ShowOptions { get; }
    public ICommand ExportImage { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    internal void PlotParameterData(Arr<LowryParameterMeasure> parameterMeasures)
    {
      var parameterNames = parameterMeasures.Map(pd => pd.ParameterName);

      if (parameterNames.Count == _lowryStackAxis.Labels.Count)
      {
        for (var i = 0; i < parameterNames.Count; ++i)
        {
          _lowryStackAxis.Labels[i] = parameterNames[i];
        }
      }
      else
      {
        _lowryStackAxis.Labels.Clear();
        foreach (var parameterName in parameterNames)
        {
          _lowryStackAxis.Labels.Add(parameterName);
        }
      }

      if (parameterMeasures.Count == _mainEffectsSeries.Items.Count)
      {
        for (var i = 0; i < parameterMeasures.Count; ++i)
        {
          _mainEffectsSeries.Items[i].Value = parameterMeasures[i].MainEffect;
          _interactionsSeries.Items[i].Value = parameterMeasures[i].Interaction;
        }
      }
      else
      {
        _mainEffectsSeries.Items.Clear();
        _interactionsSeries.Items.Clear();
        foreach (var parameterDatum in parameterMeasures)
        {
          _mainEffectsSeries.Items.Add(new ColumnItem { Value = parameterDatum.MainEffect });
          _interactionsSeries.Items.Add(new ColumnItem { Value = parameterDatum.Interaction });
        }
      }

      _smokeSeries.Points2.Clear();
      _smokeSeries.Points.Clear();
      for (var i = 0; i < parameterMeasures.Count; ++i)
      {
        _smokeSeries.Points2.Add(new DataPoint(0.5 + i, parameterMeasures[i].LowerBound));
        _smokeSeries.Points.Add(new DataPoint(0.5 + i, parameterMeasures[i].UpperBound));
      }

      _lowrySmokeAxis.Maximum = parameterMeasures.Count;

      PlotModel.InvalidatePlot(true);
    }

    internal void Clear()
    {
      for (var i = 0; i < _lowryStackAxis.Labels.Count; ++i)
      {
        _lowryStackAxis.Labels[i] = default;
      }

      for (var i = 0; i < _mainEffectsSeries.Items.Count; ++i)
      {
        _mainEffectsSeries.Items[i].Value = default;
      }

      for (var i = 0; i < _interactionsSeries.Items.Count; ++i)
      {
        _interactionsSeries.Items[i].Value = default;
      }

      _smokeSeries.Points2.Clear();
      _smokeSeries.Points.Clear();
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

    private void HandleInteractionsSeriesMouseDown(object? sender, OxyMouseDownEventArgs e)
    {
      _lastElementRightClick = INTERACTION_ELEMENT;
    }

    private void HandleMainEffectsSeriesMouseDown(object? sender, OxyMouseDownEventArgs e)
    {
      _lastElementRightClick = MAIN_EFFECT_ELEMENT;
    }

    private void HandlePlotModelMouseDown(object? sender, OxyMouseDownEventArgs e)
    {
      var dataPoint = _smokeSeries.InverseTransform(e.Position);

      if (e.ChangedButton == OxyMouseButton.Left)
      {
        var maximumVisibleEffect = _lowryVerticalAxis.ActualMaximum;
        var maximumVisibleSmoke = _lowrySmokeAxis.ActualMaximum;

        if (dataPoint.Y > maximumVisibleEffect || dataPoint.Y < 0.0 || dataPoint.X > maximumVisibleSmoke || dataPoint.X < 0.0)
        {
          _lastElementRightClick = INTERACTION_ELEMENT;
          ShowOptionsDialog(LABELS_PAGE);
          e.Handled = true;
        }
      }
      else if (e.ChangedButton == OxyMouseButton.Right)
      {
        var trackerHitResult = _smokeSeries.GetNearestPoint(e.Position, false);
        if (dataPoint.Y > trackerHitResult.DataPoint.Y)
        {
          _lastElementRightClick = SMOKE_ELEMENT;
        }
      }
    }

    private void HandleUpdateSize() =>
      _appService.ScheduleLowPriorityAction(() => SetSize());

    private void HandleResetAxes()
    {
      PlotModel.ResetAllAxes();
      PlotModel.InvalidatePlot(false);
    }

    private void HandleShowOptions() =>
      ShowOptionsDialog(CHART_PAGE);

    private void HandleExportImage()
    {
      var didSave = _appService.SaveFile(
        "Export Lowry Plot",
        _initialSaveDirectory,
        "TIFF Files|*.tiff",
        "tiff",
        out string? pathToFile
        );

      if (!didSave) return;

      using (var stream = new FileStream(pathToFile!, FileMode.Create))
      {
        TiffExporter.Export(PlotModel, stream, Width, Height, OxyColors.White, 300);
      }

      _initialSaveDirectory = GetDirectoryName(pathToFile);
    }

    private void ObserveAppSettingsPropertyChange(string? propertyName)
    {
      if (!propertyName.IsThemeProperty()) return;

      PlotModel.ApplyThemeToPlotModelAndAxes();
      PlotModel.InvalidatePlot(false);
    }

    private void SetSize()
    {
      Width = ToInt32(PlotModel.Width * 300d / 96d);
      Height = ToInt32(PlotModel.Height * 300d / 96d);
    }

    private void ShowOptionsDialog(int page)
    {
      var interactionColor = _interactionsSeries.FillColor;
      var interactionColorIndex = OxyColorData.OxyColors.FindIndex(ocd => ocd.OxyColor == interactionColor);

      var mainEffectColor = _mainEffectsSeries.FillColor;
      var mainEffectColorIndex = OxyColorData.OxyColors.FindIndex(ocd => ocd.OxyColor == mainEffectColor);

      var smokeColor = _smokeSeries.Fill;
      var smokeColorIndex = OxyColorData.OxyColors.FindIndex(ocd => ocd.OxyColor == smokeColor);

      var viewModel = new ChartOptionsViewModel(_appService)
      {
        Page = page,
        ChartTitle = PlotModel.Title,
        YAxisTitle = _lowryVerticalAxis.Title,
        XAxisTitle = _lowryStackAxis.Title,
        ElementNames = _elementNames,
        ElementColorIndices = new[] { interactionColorIndex, mainEffectColorIndex, smokeColorIndex },
        WindowTitle = "Lowry Plot Options",
        SelectedElement = _lastElementRightClick,
        ShowAxesTab = false
      };

      var ok = _appService.ShowDialog(new ChartOptionsDialog(), viewModel, default);

      if (ok)
      {
        PlotModel.Title = viewModel.ChartTitle;
        _lowryVerticalAxis.Title = viewModel.YAxisTitle;
        _lowryStackAxis.Title = viewModel.XAxisTitle;
        _interactionsSeries.FillColor = OxyColorData.OxyColors[viewModel.ElementColorIndices[INTERACTION_ELEMENT]].OxyColor;
        _mainEffectsSeries.FillColor = OxyColorData.OxyColors[viewModel.ElementColorIndices[MAIN_EFFECT_ELEMENT]].OxyColor;
        _smokeSeries.Fill = OxyColorData.OxyColors[viewModel.ElementColorIndices[SMOKE_ELEMENT]].OxyColor;
        PlotModel.InvalidatePlot(false);

        _lowryState.ChartTitle = PlotModel.Title;
        _lowryState.XAxisTitle = _lowryStackAxis.Title;
        _lowryState.YAxisTitle = _lowryVerticalAxis.Title;
        _lowryState.InteractionsFillColor = _interactionsSeries.FillColor;
        _lowryState.MainEffectsFillColor = _mainEffectsSeries.FillColor;
        _lowryState.SmokeFill = _smokeSeries.Fill;
      }
    }

    private readonly Arr<string> _elementNames = Array("Interaction", "Main Effect", "Smoke");
    private const int INTERACTION_ELEMENT = 0;
    private const int MAIN_EFFECT_ELEMENT = 1;
    private const int SMOKE_ELEMENT = 2;

    private readonly IAppService _appService;
    private readonly IAppSettings _appSettings;
    private readonly LowryState _lowryState;
    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
    private readonly IDisposable _subscriptions;
    private readonly ColumnSeries _mainEffectsSeries;
    private readonly ColumnSeries _interactionsSeries;
    private readonly AreaSeries _smokeSeries;
    private readonly CategoryAxis _lowryStackAxis;
    private readonly LinearAxis _lowrySmokeAxis;
    private readonly LinearAxis _lowryVerticalAxis;
    private string? _initialSaveDirectory;
    private int _lastElementRightClick;
    private bool _disposed = false;
  }
}
