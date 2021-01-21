using LanguageExt;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Windows.Input;
using static LanguageExt.Prelude;
using static System.Math;

#nullable disable

namespace Sensitivity.Design
{
  internal sealed class Fast99MeasuresViewModel : IFast99MeasuresViewModel
  {
    public bool IsVisible => true;

    public bool IsReady { get => true; set => throw new NotImplementedException(); }

    public bool IsSelected { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public Arr<string> OutputNames => Range(1, 20).Map(i => $"name{i:000}").ToArr();

    public int SelectedOutputName { get => 3; set => throw new NotImplementedException(); }

    public Fast99MeasureType Fast99MeasureType { get => Fast99MeasureType.TotalEffect; set => throw new NotImplementedException(); }

    public string XUnits => "secs.secs.secs.secs.secs";

    public string XBeginText { get => "5.4321!!!"; set => throw new NotImplementedException(); }
    public double? XBegin { get => 1.23456; }

    public string XEndText { get => "9.8765!!!"; set => throw new NotImplementedException(); }
    public double? XEnd { get => 4.56789; }

    public Arr<IRankedParameterViewModel> RankedParameterViewModels =>
      Range(1, 20)
        .Map(i => new RankedParameterViewModel($"parameter {i:000}", i) { IsSelected = i % 2 != 0 })
        .ToArr<IRankedParameterViewModel>();

    public Arr<string> RankedUsing => Range(1, 20).Map(i => $"output {i:000}").ToArr();

    public double? RankedFrom => 5.4321;

    public double? RankedTo => 9.8765;

    public ICommand RankParameters => throw new NotImplementedException();

    public ICommand UseRankedParameters => throw new NotImplementedException();

    public ICommand ShareRankedParameters => throw new NotImplementedException();

    public PlotModel PlotModel => CreatePlotModel();

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
