using RVisUI.Model.Extensions;
using System.ComponentModel;
using System.Windows.Input;

namespace Estimation
{
  internal sealed class OutputViewModel : IOutputViewModel, INotifyPropertyChanged
  {
    internal OutputViewModel(string name, ICommand toggleSelect)
    {
      Name = name;
      ToggleSelect = toggleSelect;
      SortKey = name.ToUpperInvariant();
    }

    public string Name { get; }

    public string? ErrorModel
    {
      get => _errorModel;
      set => this.RaiseAndSetIfChanged(ref _errorModel, value, PropertyChanged);
    }
    private string? _errorModel;

    public bool IsSelected
    {
      get => _isSelected;
      set => this.RaiseAndSetIfChanged(ref _isSelected, value, PropertyChanged);
    }
    private bool _isSelected;

    public ICommand ToggleSelect { get; }

    public string SortKey { get; }

    public event PropertyChangedEventHandler? PropertyChanged;
  }
}
