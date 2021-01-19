using RVisUI.Model.Extensions;
using System.ComponentModel;

namespace Sampling
{
  internal sealed class SamplesState : INotifyPropertyChanged
  {
    internal int? NumberOfSamples
    {
      get => _numberOfSamples;
      set => this.RaiseAndSetIfChanged(ref _numberOfSamples, value, PropertyChanged);
    }
    private int? _numberOfSamples;

    internal int? Seed
    {
      get => _seed;
      set => this.RaiseAndSetIfChanged(ref _seed, value, PropertyChanged);
    }
    private int? _seed;

    internal LatinHypercubeDesign LatinHypercubeDesign
    {
      get => _latinHypercubeDesign;
      set => this.RaiseAndSetIfChanged(ref _latinHypercubeDesign, value, PropertyChanged);
    }
    private LatinHypercubeDesign _latinHypercubeDesign = LatinHypercubeDesign.Default;

    internal RankCorrelationDesign RankCorrelationDesign
    {
      get => _rankCorrelationDesign;
      set => this.RaiseAndSetIfChanged(ref _rankCorrelationDesign, value, PropertyChanged);
    }
    private RankCorrelationDesign _rankCorrelationDesign;

    public event PropertyChangedEventHandler? PropertyChanged;
  }
}
