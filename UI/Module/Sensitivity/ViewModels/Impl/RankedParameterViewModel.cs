using RVisUI.Model.Extensions;
using System.ComponentModel;

namespace Sensitivity
{
  internal sealed class RankedParameterViewModel : IRankedParameterViewModel, INotifyPropertyChanged
  {
    internal RankedParameterViewModel(string name, double score)
    {
      Name = name;
      Score = score;
    }

    public string Name { get; }

    public double Score { get; }

    public bool IsSelected 
    {
      get => _isSelected;
      set => this.RaiseAndSetIfChanged(ref _isSelected, value, PropertyChanged);
    }
    private bool _isSelected;

    public event PropertyChangedEventHandler? PropertyChanged;
  }
}
