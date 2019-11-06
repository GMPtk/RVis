using LanguageExt;
using MaterialDesignColors;
using System.Windows.Input;

namespace RVisUI.Ioc.Mvvm
{
  internal sealed class SwatchViewModel : ISwatchViewModel
  {
    internal SwatchViewModel(ISwatch swatch, ICommand changeHue)
    {
      Swatch = swatch;

      HueViewModels = swatch.Hues
        .Map((i, h) => new HueViewModel(i, h, swatch, changeHue))
        .ToArr<IHueViewModel>();
    }

    public ISwatch Swatch { get; }

    public Arr<IHueViewModel> HueViewModels { get; }
  }
}
