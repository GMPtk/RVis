using RVisUI.Model.Extensions;
using System.ComponentModel;

namespace Estimation
{
  internal sealed class PriorsState : INotifyPropertyChanged
  {
    internal string? SelectedPrior
    {
      get => _selectedPrior;
      set => this.RaiseAndSetIfChanged(ref _selectedPrior, value, PropertyChanged);
    }
    private string? _selectedPrior;

    public event PropertyChangedEventHandler? PropertyChanged;
  }
}
