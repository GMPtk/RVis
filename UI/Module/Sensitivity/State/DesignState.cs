using RVisUI.Model.Extensions;
using System.ComponentModel;

namespace Sensitivity
{
  internal sealed class DesignState : INotifyPropertyChanged
  {
    internal int? SampleSize
    {
      get => _sampleSize;
      set => this.RaiseAndSetIfChanged(ref _sampleSize, value, PropertyChanged);
    }
    private int? _sampleSize;

    internal string SelectedElementName
    {
      get => _selectedElementName;
      set => this.RaiseAndSetIfChanged(ref _selectedElementName, value, PropertyChanged);
    }
    private string _selectedElementName;

    public event PropertyChangedEventHandler PropertyChanged;
  }
}
