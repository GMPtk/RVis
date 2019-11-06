using RVisUI.Model.Extensions;
using System.ComponentModel;

namespace Sampling
{
  internal sealed class DesignState : INotifyPropertyChanged
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

    internal string SelectedElementName
    {
      get => _selectedElementName;
      set => this.RaiseAndSetIfChanged(ref _selectedElementName, value, PropertyChanged);
    }
    private string _selectedElementName;

    public event PropertyChangedEventHandler PropertyChanged;
  }
}
