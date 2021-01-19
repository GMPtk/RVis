using RVisUI.Model.Extensions;
using System.ComponentModel;

namespace Sensitivity
{
  internal sealed class OutputViewModel : IOutputViewModel, INotifyPropertyChanged
  {
    internal OutputViewModel(string name)
    {
      Name = name;
    }

    public string Name { get; }

    public bool IsSelected 
    {
      get => _isSelected;
      set => this.RaiseAndSetIfChanged(ref _isSelected, value, PropertyChanged);
    }
    private bool _isSelected;

    public event PropertyChangedEventHandler? PropertyChanged;
  }
}
