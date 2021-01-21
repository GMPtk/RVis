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
      var plotModel = new PlotModel()
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
      histogramSeries.Items.Add(new HistogramItem(0, 0.3333333333333333, 0.2874, 2874));
      histogramSeries.Items.Add(new HistogramItem(0.3333333333333333, 0.6666666666666666, 0.204, 2040));
      histogramSeries.Items.Add(new HistogramItem(0.6666666666666666, 1, 0.1384, 1384));
      histogramSeries.Items.Add(new HistogramItem(1, 1.3333333333333333, 0.1092, 1092));
      histogramSeries.Items.Add(new HistogramItem(1.3333333333333333, 1.6666666666666667, 0.0755, 755));
      histogramSeries.Items.Add(new HistogramItem(1.6666666666666667, 2, 0.0534, 534));
      histogramSeries.Items.Add(new HistogramItem(2, 2.3333333333333335, 0.0374, 374));
      histogramSeries.Items.Add(new HistogramItem(2.3333333333333335, 2.6666666666666665, 0.0288, 288));
      histogramSeries.Items.Add(new HistogramItem(2.6666666666666665, 3, 0.02, 200));
      histogramSeries.Items.Add(new HistogramItem(3, 3.3333333333333335, 0.0115, 115));
      histogramSeries.Items.Add(new HistogramItem(3.3333333333333335, 3.6666666666666665, 0.0104, 104));
      histogramSeries.Items.Add(new HistogramItem(3.6666666666666665, 4, 0.0061, 61));
      histogramSeries.Items.Add(new HistogramItem(4, 4.333333333333333, 0.0052, 52));
      histogramSeries.Items.Add(new HistogramItem(4.333333333333333, 4.666666666666667, 0.0033, 33));
      histogramSeries.Items.Add(new HistogramItem(4.666666666666667, 5, 0.0029, 29));
      plotModel.Series.Add(histogramSeries);

      return plotModel;
    }
  }
}
