using LanguageExt;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Windows.Input;
using static LanguageExt.Prelude;
using static System.Math;

namespace Estimation.Design
{
  internal sealed class SimulationViewModel : ISimulationViewModel
  {
    public ICommand StartIterating => throw new NotImplementedException();

    public bool CanStartIterating { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public ICommand StopIterating => throw new NotImplementedException();

    public bool CanStopIterating { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public ICommand ShowSettings => throw new NotImplementedException();

    public bool CanShowSettings { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public PlotModel PlotModel => CreatePlotModel();

    public int? PosteriorBegin { get => 123; set => throw new NotImplementedException(); }

    public int? PosteriorEnd { get => 456; set => throw new NotImplementedException(); }

    public bool CanAdjustConvergenceRange { get => true; set => throw new NotImplementedException(); }

    public ICommand SetConvergenceRange => throw new NotImplementedException();

    public bool CanSetConvergenceRange { get => false; set => throw new NotImplementedException(); }

    public Arr<string> Parameters => Range(1, 20).Map(i => $"name{i:000}").ToArr();

    public int SelectedParameter { get => 3; set => throw new NotImplementedException(); }

    public bool IsVisible { get => true; set => throw new NotImplementedException(); }

    private static PlotModel CreatePlotModel()
    {
      var plotModel = new PlotModel()
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
