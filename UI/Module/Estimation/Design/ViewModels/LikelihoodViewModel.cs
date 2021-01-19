using LanguageExt;
using OxyPlot;
using RVisUI.AppInf.Extensions;
using System.Collections.ObjectModel;
using System.Linq;
using static LanguageExt.Prelude;
using static System.Double;

#nullable disable

namespace Estimation.Design
{
  internal sealed class LikelihoodViewModel : ILikelihoodViewModel
  {
    public Arr<IOutputViewModel> AllOutputViewModels => Range(1, 30)
      .Map(i => new OutputViewModel($"Output{i:0000}", default) { IsSelected = 0 == i % 2 })
      .ToArr<IOutputViewModel>();

    public ObservableCollection<IOutputViewModel> SelectedOutputViewModels
    {
      get => new ObservableCollection<IOutputViewModel>(Range(1, 30)
        .Map(i => new OutputViewModel($"Output{i:0000}", default)
        {
          IsSelected = true,
          ErrorModel = $"xxx ~ Normal({i},{i * 2})"
        }));
    }

    public int SelectedOutputViewModel { get => 5; set => throw new System.NotImplementedException(); }

    public IOutputErrorViewModel OutputErrorViewModel => new OutputErrorViewModel();

    public Arr<IObservationsViewModel> ObservationsViewModels
    {
      get => Range(1, 100)
        .Select(i => new ObservationsViewModel(
          i,
          $"subject{i:0000}",
          $"refName{i:0000}",
          $"source{i:0000}",
          Range(i + i, i * i).Select(j => j + 1.0).ToArr(),
          Range(i + i, i * i).Select(j => j + 2.0).ToArr()
          )
        { IsSelected = i % 2 == 0 })
        .ToArr<IObservationsViewModel>();
      set => throw new System.NotImplementedException();
    }
    public PlotModel PlotModel
    {
      get => _plotModel;
      set => throw new System.NotImplementedException();
    }
    public bool IsVisible { get => false; set => throw new System.NotImplementedException(); }

    private readonly PlotModel _plotModel = PlotSeries(
      "time [s]",
      "CV2 [mg/L]",
      Range(1, 20).Map(i =>
        (
          $"serie {i}",
          Range(i * i, i + 5).Map(j => j + i * 2.0).ToArr(),
          Range(i, i + 5).Map(j => j + i * 3.0).ToArr()
        )
      ).ToArr()
      );

    private static PlotModel PlotSeries(
      string independentVariable,
      string dependentVariable,
      Arr<(string SerieName, Arr<double> X, Arr<double> Y)> series
    )
    {
      var plotModel = new PlotModel();

      var xMin = series.Min(s => s.X.Min());
      var xMax = series.Max(s => s.X.Max());

      var horizontalAxisMin = xMax > 0.0 && xMin > 0.0 && (xMax - xMin) > xMin
        ? 0.0
        : NaN;

      var yMin = series.Min(s => s.Y.Min());
      var yMax = series.Max(s => s.Y.Max());

      var verticalAxisMin = yMax > 0.0 && yMin > 0.0 && (yMax - yMin) > yMin
        ? 0.0
        : NaN;

      plotModel.AddAxes(
        independentVariable,
        horizontalAxisMin,
        default,
        dependentVariable,
        verticalAxisMin,
        default
        );

      series.Iter((i, s) => plotModel.AddScatterSeries(
        i,
        s.SerieName,
        s.X,
        s.Y,
        plotModel.DefaultColors,
        default,
        default
        ));

      return plotModel;
    }
  }
}
