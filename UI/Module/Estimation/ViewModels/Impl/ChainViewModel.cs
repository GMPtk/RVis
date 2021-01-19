using RVisUI.Model.Extensions;
using System.ComponentModel;

namespace Estimation
{
  internal sealed class ChainViewModel : IChainViewModel, INotifyPropertyChanged
  {
    internal ChainViewModel(int no)
    {
      No = no;
    }

    public int No { get; }

    public bool IsSelected
    {
      get => _isSelected;
      set => this.RaiseAndSetIfChanged(ref _isSelected, value, PropertyChanged);
    }
    private bool _isSelected;

    public event PropertyChangedEventHandler? PropertyChanged;
  }
}
