using RVisUI.Model.Extensions;
using System.ComponentModel;

namespace Estimation
{
  internal sealed class LikelihoodState : INotifyPropertyChanged
  {
    internal string? SelectedOutput
    {
      get => _selectedOutput;
      set => this.RaiseAndSetIfChanged(ref _selectedOutput, value, PropertyChanged);
    }
    private string? _selectedOutput;

    public event PropertyChangedEventHandler? PropertyChanged;
  }
}
