using LanguageExt;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using RVisUI.AppInf.Design;
using RVisUI.AppInf.Extensions;
using RVisUI.Model;
using RVisUI.Model.Extensions;
using System;
using System.Reactive.Disposables;

namespace Sensitivity
{
  internal sealed class MuStarSigmaViewModel : IMuStarSigmaViewModel, IDisposable
  {
    internal MuStarSigmaViewModel()
      : this(new AppService(), new AppSettings())
    {
    }

    internal MuStarSigmaViewModel(IAppService appService, IAppSettings appSettings)
    {
      _appSettings = appSettings;

      PlotModel = new PlotModel
      {
        IsLegendVisible = false
      };

      _horizontalAxis = new LinearAxis
      {
        Title = "µ*",
        Position = AxisPosition.Bottom
      };
      PlotModel.Axes.Add(_horizontalAxis);

      _verticalAxis = new LinearAxis
      {
        Title = "σ",
        Position = AxisPosition.Left
      };
      PlotModel.Axes.Add(_verticalAxis);

      PlotModel.ApplyThemeToPlotModelAndAxes();

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

    internal void SetBounds(double hMin, double hMax, double vMin, double vMax)
    {
      _horizontalAxis.Minimum = hMin;
      _horizontalAxis.Maximum = hMax;
      _verticalAxis.Minimum = vMin;
      _verticalAxis.Maximum = vMax;
    }

    internal void Plot(Arr<MuStarSigmaParameterMeasure> parameterMeasures)
    {
      var isUpdate = PlotModel.Annotations.Count > 0;

      if (!isUpdate)
      {
        PlotModel.DefaultColors = GetPalette(parameterMeasures.Count).Colors;
      }

      PlotModel.Annotations.Clear();

      parameterMeasures.Iter((i, t) =>
      {
        PlotModel.Annotations.Add(
          new PointAnnotation
          {
            X = t.MuStar,
            Y = t.Sigma,
            Shape = MarkerType.Circle,
            Fill = PlotModel.DefaultColors[i % PlotModel.DefaultColors.Count],
            StrokeThickness = 1,
            Text = t.ParameterName,
            TextHorizontalAlignment = HorizontalAlignment.Left,
            TextVerticalAlignment = VerticalAlignment.Middle
          });
      });

      PlotModel.ResetAllAxes();

      PlotModel.InvalidatePlot(updateData: true);
    }

    internal void Clear()
    {
      PlotModel.Annotations.Clear();
      PlotModel.InvalidatePlot(updateData: true);
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

    private void ObserveAppSettingsPropertyChange(string? propertyName)
    {
      if (!propertyName.IsThemeProperty()) return;

      PlotModel.ApplyThemeToPlotModelAndAxes();
      PlotModel.InvalidatePlot(false);
    }

    private OxyPalette GetPalette(int numberOfColors) =>
      _appSettings.IsBaseDark
        ? OxyPalettes.Cool(numberOfColors)
        : OxyPalettes.Rainbow(numberOfColors);

    private readonly IAppSettings _appSettings;
    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
    private readonly IDisposable _subscriptions;
    private readonly LinearAxis _horizontalAxis;
    private readonly LinearAxis _verticalAxis;
    private bool _disposed = false;
  }
}
