using MaterialDesignColors;
using RVisUI.Model.Extensions;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Media;

namespace RVisUI.Ioc.Mvvm
{
  internal sealed class HueViewModel : IHueViewModel, INotifyPropertyChanged
  {
    internal HueViewModel(int hueIndex, Color hue, ISwatch swatch, ICommand changeHue)
    {
      HueIndex = hueIndex;
      Hue = hue;
      Swatch = swatch;
      ChangeHue = changeHue;
    }

    public int HueIndex { get; }

    public Color Hue { get; }

    public ISwatch Swatch { get; }

    public ICommand ChangeHue { get; }

    public bool IsSelected
    {
      get => _isSelected;
      set => this.RaiseAndSetIfChanged(ref _isSelected, value, PropertyChanged);
    }
    private bool _isSelected;

    public event PropertyChangedEventHandler? PropertyChanged;
  }
}
