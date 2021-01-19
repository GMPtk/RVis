using LanguageExt;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;
using System;
using System.Collections.ObjectModel;
using static LanguageExt.Prelude;

namespace Estimation.Design
{
  internal sealed class FitViewModel : IFitViewModel
  {
    public Arr<IChainViewModel> ChainViewModels => Range(1, 10)
      .Map(i => new ChainViewModel(i) { IsSelected = i % 2 == 0 })
      .ToArr<IChainViewModel>();

    public Arr<string> OutputNames => Range(1, 10)
      .Map(i => $"A very, very, very, long output name {i:000}")
      .ToArr();

    public int SelectedOutputName
    {
      get => 2;
      set => throw new NotImplementedException();
    }

    public PlotModel PlotModel => CreatePlotModel();

    public bool IsVisible
    {
      get => throw new NotImplementedException();
      set => throw new NotImplementedException();
    }

    private static PlotModel CreatePlotModel()
    {
      var items = new Collection<BoxPlotItem>
      {
        new BoxPlotItem(1, 13.0, 15.5, 17.0, 18.5, 19.5) { Mean = 18.0 },
        new BoxPlotItem(2, 13.0, 15.5, 17.0, 18.5, 19.5),
        new BoxPlotItem(3, 12.0, 13.5, 15.5, 18.0, 20.0) { Mean = 14.5 },
        new BoxPlotItem(4, 12.0, 13.5, 15.5, 18.0, 20.0) { Mean = 14.5 },
        new BoxPlotItem(5, 13.5, 14.0, 14.5, 15.5, 16.5)
      };

      var plotModel = new PlotModel();

      plotModel.Legends.Add(new Legend
      {
        LegendPlacement = LegendPlacement.Inside,
        LegendPosition = LegendPosition.RightTop,
        LegendOrientation = LegendOrientation.Vertical
      });

      plotModel.Axes.Add(new LinearAxis { Title = "X Axis", Position = AxisPosition.Bottom });
      plotModel.Axes.Add(new LinearAxis { Title = "Y Axis", Position = AxisPosition.Left });

      plotModel.Series.Add(new BoxPlotSeries { Title = "Values", ItemsSource = items });

      plotModel.Series.Add(new ScatterSeries() 
      { 
        Title = "Data", 
        ItemsSource = Range(1, 5).Map(i => new ScatterPoint(i, 10 + i * 2)) 
      });

      return plotModel;
    }
  }
}
