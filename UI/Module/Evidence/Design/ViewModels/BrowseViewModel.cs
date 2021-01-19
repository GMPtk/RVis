using LanguageExt;
using OxyPlot;
using RVisUI.AppInf.Extensions;
using System;
using System.Linq;
using static LanguageExt.Prelude;
using static System.Double;
using static System.Linq.Enumerable;

#nullable disable

namespace Evidence.Design
{
  internal class BrowseViewModel : IBrowseViewModel
  {
    public PlotModel PlotModel
    {
      get => _plotModel;
      set => throw new NotImplementedException();
    }
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

    public Arr<ISubjectViewModel> SubjectViewModels
    {
      get => _subjectViewModels;
      set => throw new NotImplementedException();
    }
    private readonly Arr<ISubjectViewModel> _subjectViewModels = Range(1, 100)
      .Select(i => new SubjectViewModel($"{i:0000} followed by some text, text, text, text") { NSelected = i, NAvailable = i + 3 })
      .ToArr<ISubjectViewModel>();

    public ISubjectViewModel SelectedSubjectViewModel
    {
      get => _subjectViewModels[1];
      set => throw new NotImplementedException();
    }

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
      set => throw new NotImplementedException();
    }

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
