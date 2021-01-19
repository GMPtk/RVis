using RVisUI.Model.Extensions;
using System.ComponentModel;

namespace Sampling
{
  internal sealed class ParametersState : INotifyPropertyChanged
  {
    internal string? SelectedParameter
    {
      get => _selectedParameter;
      set => this.RaiseAndSetIfChanged(ref _selectedParameter, value, PropertyChanged);
    }
    private string? _selectedParameter;

    public event PropertyChangedEventHandler? PropertyChanged;
  }
}
