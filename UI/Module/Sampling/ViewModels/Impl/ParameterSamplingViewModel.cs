using LanguageExt;
using MathNet.Numerics.Statistics;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using ReactiveUI;
using RVis.Model;
using RVisUI.AppInf.Extensions;
using RVisUI.Model.Extensions;
using System;
using System.ComponentModel;
using static RVis.Base.Check;

namespace Sampling
{
  internal sealed class ParameterSamplingViewModel : IParameterSamplingViewModel, INotifyPropertyChanged
  {
    internal ParameterSamplingViewModel(SimParameter parameter)
    {
      RequireNotNull(parameter);

      Parameter = parameter;
      SortKey = parameter.Name.ToUpperInvariant();

      var histogram = new PlotModel();

      var linearAxis = new LinearAxis
      {
        Title = "Frequency"
      };
      histogram.Axes.Add(linearAxis);

      linearAxis = new LinearAxis
      {
        Position = AxisPosition.Bottom,
        Title = parameter.Name,
        Unit = parameter.Unit
      };

      histogram.Axes.Add(linearAxis);

      Histogram = histogram;

      this
        .ObservableForProperty(vm => vm.Distribution)
        .Subscribe(ObserveDistribution);

      this
        .ObservableForProperty(vm => vm.Samples)
        .Subscribe(ObserveSamples);
    }

    public SimParameter Parameter { get; }

    public string SortKey { get; }

    public IDistribution Distribution
    {
      get => _distribution;
      set => this.RaiseAndSetIfChanged(ref _distribution, value, PropertyChanged);
    }
    private IDistribution _distribution;

    public Arr<double> Samples
    {
      get => _samples;
      set => this.RaiseAndSetIfChanged(ref _samples, value, PropertyChanged);
    }
    private Arr<double> _samples;

    public PlotModel Histogram { get; }

    public event PropertyChangedEventHandler PropertyChanged;

    private void ObserveDistribution(object _)
    {
      Samples = default;

      Histogram.Series.Clear();

      Histogram.Title = Distribution?.ToString(Parameter.Name);

      Histogram.InvalidatePlot(true);
    }

    private void ObserveSamples(object _)
    {
      Histogram.Series.Clear();

      if (!Samples.IsEmpty)
      {
        var (_, nBins) = Samples.FreedmanDiaconis();
        var histogram = new Histogram(Samples, nBins);

        var histogramSeries = new HistogramSeries
        {
          StrokeThickness = 1
        };

        for (var i = 0; i < histogram.BucketCount; ++i)
        {
          var bucket = histogram[i];
          histogramSeries.Items.Add(
            new HistogramItem(
              bucket.LowerBound,
              bucket.UpperBound,
              bucket.Count / histogram.DataCount
              )
            );
        }

        Histogram.Series.Add(histogramSeries);
      }

      Histogram.InvalidatePlot(true);
    }
  }
}
