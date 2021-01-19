using LanguageExt;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Windows.Input;
using static LanguageExt.Prelude;
using static System.Math;

namespace Sampling.Design
{
  internal sealed class OutputsViewModel : IOutputsViewModel
  {
    public bool IsVisible { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public Arr<string> OutputNames => Range(1, 20).Map(i => $"name{i:000}").ToArr();
    public int SelectedOutputName { get => 3; set => throw new NotImplementedException(); }

    public PlotModel Outputs => CreatePlotModel();

    public PlotController PlotController => new PlotController();

    public ICommand ToggleSeriesType => throw new NotImplementedException();

    public bool IsSeriesTypeLine => true;

    public ICommand ResetAxes => throw new NotImplementedException();

    public IOutputsSelectedSampleViewModel OutputsSelectedSampleViewModel { get; } = new OutputsSelectedSampleViewModel();

    public IOutputsFilteredSamplesViewModel OutputsFilteredSamplesViewModel { get; } = new OutputsFilteredSamplesViewModel();

    public IOutputsEvidenceViewModel OutputsEvidenceViewModel { get; } = new OutputsEvidenceViewModel();

    private static PlotModel CreatePlotModel()
    {
      var plotModel = new PlotModel
      {
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
        Title = "some output name"
      };

      plotModel.Axes.Add(verticalAxis);

      const int count = 101;
      var x = Range(0, count).Map(i => -PI + 2 * PI * i / 100).ToArr();

      var sin = x.Map(Sin);
      var cos = x.Map(Cos);

      var series = new LineSeries { StrokeThickness = 4 };

      for (var i = 0; i < count; ++i)
      {
        series.Points.Add(new DataPoint(x[i], sin[i]));
      }

      plotModel.Series.Add(series);

      series = new LineSeries { StrokeThickness = 4 };

      for (var i = 0; i < count; ++i)
      {
        series.Points.Add(new DataPoint(x[i], cos[i]));
      }

      plotModel.Series.Add(series);

      return plotModel;
    }
  }
}
