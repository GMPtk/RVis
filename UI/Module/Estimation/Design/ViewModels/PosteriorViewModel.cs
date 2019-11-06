using LanguageExt;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using static LanguageExt.Prelude;
using static System.Math;

namespace Estimation.Design
{
  internal sealed class PosteriorViewModel : IPosteriorViewModel
  {
    public Arr<IChainViewModel> ChainViewModels => Range(1, 10)
      .Map(i => new ChainViewModel(i) { IsSelected = i % 2 == 0 })
      .ToArr<IChainViewModel>();

    public Arr<string> ParameterNames => Range(1, 10)
      .Map(i => $"A very, very, very, long parameter name {i:000}")
      .ToArr();

    public int SelectedParameterName
    {
      get => 2;
      set => throw new NotImplementedException();
    }

    public PlotModel PlotModel => CreatePlotModel();

    public double? AcceptRate { get => 0.6543; set => throw new NotImplementedException(); }

    public bool IsVisible { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    private static PlotModel CreatePlotModel()
    {
      var plotModel = new PlotModel
      {
        LegendPlacement = LegendPlacement.Inside,
        LegendPosition = LegendPosition.RightTop,
        LegendOrientation = LegendOrientation.Vertical
      };

      plotModel.Axes.Add(new LinearAxis { Title = "X Axis", Position = AxisPosition.Bottom });
      plotModel.Axes.Add(new LinearAxis { Title = "Y Axis", Position = AxisPosition.Left });

      var lineSeries = new LineSeries() { Title = "Prior" };

      Range(0, 50).Map(i => i / 10d).Iter(d =>
      {
        lineSeries.Points.Add(new DataPoint(d, Abs(Cos(d))));
      });

      plotModel.Series.Add(lineSeries);

      var histogramSeries = new HistogramSeries
      {
        Title = "Posterior",
        StrokeThickness = 1,
        FillColor = OxyColors.Transparent
      };
      histogramSeries.Items.Add(new HistogramItem(0, 0.333333333333333, 0.2803));
      histogramSeries.Items.Add(new HistogramItem(0.333333333333333, 0.666666666666667, 0.2024));
      histogramSeries.Items.Add(new HistogramItem(0.666666666666667, 1, 0.1497));
      histogramSeries.Items.Add(new HistogramItem(1, 1.33333333333333, 0.1046));
      histogramSeries.Items.Add(new HistogramItem(1.33333333333333, 1.66666666666667, 0.0735));
      histogramSeries.Items.Add(new HistogramItem(1.66666666666667, 2, 0.0528));
      histogramSeries.Items.Add(new HistogramItem(2, 2.33333333333333, 0.0373));
      histogramSeries.Items.Add(new HistogramItem(2.33333333333333, 2.66666666666667, 0.0287));
      histogramSeries.Items.Add(new HistogramItem(2.66666666666667, 3, 0.0184));
      histogramSeries.Items.Add(new HistogramItem(3, 3.33333333333333, 0.0146));
      histogramSeries.Items.Add(new HistogramItem(3.33333333333333, 3.66666666666667, 0.0096));
      histogramSeries.Items.Add(new HistogramItem(3.66666666666667, 4, 0.0064));
      histogramSeries.Items.Add(new HistogramItem(4, 4.33333333333333, 0.0066));
      histogramSeries.Items.Add(new HistogramItem(4.33333333333333, 4.66666666666667, 0.0041));
      histogramSeries.Items.Add(new HistogramItem(4.66666666666667, 5, 0.0025));
      plotModel.Series.Add(histogramSeries);

      return plotModel;
    }
  }
}
