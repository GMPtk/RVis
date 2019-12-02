using RVisUI.Model.Extensions;
using System.ComponentModel;

namespace Sampling
{
  internal sealed class LHSParameterViewModel : ILHSParameterViewModel, INotifyPropertyChanged
  {
    internal LHSParameterViewModel(string name)
    {
      Name = name;
    }

    public string Name { get; }

    public double? Lower
    {
      get => _lower;
      set => this.RaiseAndSetIfChanged(ref _lower, value, PropertyChanged);
    }
    private double? _lower;

    public double? Upper
    {
      get => _upper;
      set => this.RaiseAndSetIfChanged(ref _upper, value, PropertyChanged);
    }
    private double? _upper;

    public event PropertyChangedEventHandler PropertyChanged;
  }
}
