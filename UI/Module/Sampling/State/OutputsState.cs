using RVisUI.Model.Extensions;
using System.ComponentModel;

namespace Sampling
{
  internal sealed class OutputsState : INotifyPropertyChanged
  {
    internal string SelectedOutputName
    {
      get => _selectedOutputName;
      set => this.RaiseAndSetIfChanged(ref _selectedOutputName, value, PropertyChanged);
    }
    private string _selectedOutputName;

    public event PropertyChangedEventHandler PropertyChanged;
  }
}
