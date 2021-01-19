using LanguageExt;
using MaterialDesignColors;
using System.Windows.Input;
using System.Windows.Media;

namespace RVisUI.Ioc.Mvvm
{
  internal interface IHueViewModel
  {
    int HueIndex { get; }
    Color Hue { get; }
    ISwatch Swatch { get; }
    ICommand ChangeHue { get; }
    bool IsSelected { get; set; }
  }

  internal interface ISwatchViewModel
  {
    ISwatch Swatch { get; }
    Arr<IHueViewModel> HueViewModels { get; }
  }

  internal interface IAppSettingsViewModel
  {
    ICommand View { get; }
    bool Show { get; set; }
    bool RestoreWindow { get; set; }

    Arr<string> CoresOptions { get; }
    int NumberOfCoresSelectedIndex { get; set; }

    bool IsBaseDark { get; set; }
    Arr<ISwatchViewModel> SwatchViewModels { get; }
    ColorScheme ActiveScheme { get; set; }

    string? SecondaryHueLightHex { get; }
    string? SecondaryHueMidHex { get; }
    string? SecondaryHueDarkHex { get; }
    string? SecondaryHueMidForegroundHex { get; }

    ICommand ChangeHue { get; }
    ICommand ChangeToPrimary { get; }
    ICommand ChangeToSecondary { get; }
    ICommand ChangeToPrimaryForeground { get; }
    ICommand ChangeToSecondaryForeground { get; }

    string ModuleConfiguration { get; set; }
  }
}
