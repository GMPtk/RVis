using LanguageExt;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using static LanguageExt.Prelude;
using static System.Math;

namespace Sensitivity.Design
{
  internal sealed class VarianceViewModel : IVarianceViewModel
  {
    public bool IsVisible => true;

    public bool IsSelected { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public Arr<string> OutputNames => Range(1, 20).Map(i => $"name{i:000}").ToArr();

    public int SelectedOutputName { get => 3; set => throw new NotImplementedException(); }

    public VarianceMeasureType VarianceMeasureType { get => VarianceMeasureType.TotalEffect; set => throw new NotImplementedException(); }

    public PlotModel PlotModel => CreatePlotModel();

    private static PlotModel CreatePlotModel()
    {
      var plotModel = new PlotModel
      {
        LegendPosition = LegendPosition.RightMiddle,
        LegendPlacement = LegendPlacement.Outside
      };

      var horizontalAxis = new LinearAxis
      {
        Position = AxisPosition.Bottom,
        Title = "time"
      };

      plotModel.Axes.Add(horizontalAxis);

      var verticalAxis = new LinearAxis
      {
        Position = AxisPosition.Left,
        Title = "Main Effect"
      };

      plotModel.Axes.Add(verticalAxis);

      const int count = 101;
      var x = Range(0, count).Map(i => -PI + 2 * PI * i / 100).ToArr();

      var sin = x.Map(Sin);
      var cos = x.Map(Cos);

      var series = new LineSeries { Title = "Sin", StrokeThickness = 4 };

      for (var i = 0; i < count; ++i)
      {
        series.Points.Add(new DataPoint(x[i], sin[i]));
      }

      plotModel.Series.Add(series);

      series = new LineSeries { Title = "Cos", StrokeThickness = 4 };

      for (var i = 0; i < count; ++i)
      {
        series.Points.Add(new DataPoint(x[i], cos[i]));
      }

      plotModel.Series.Add(series);

      return plotModel;
    }
  }
}
