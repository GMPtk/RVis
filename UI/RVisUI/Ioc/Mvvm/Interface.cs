using LanguageExt;
using MaterialDesignColors;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using System.Windows.Input;

namespace RVisUI.Ioc.Mvvm
{
  internal interface IAppSettingsViewModel
  {
    bool RestoreWindow { get; set; }

    Arr<string> CoresOptions { get; }
    int NumberOfCoresSelectedIndex { get; set; }

    bool IsBaseDark { get; set; }
    bool IsColorAdjusted { get; set; }
    float DesiredContrastRatio { get; set; }
    IEnumerable<Contrast> ContrastValues { get; }
    Contrast ContrastValue { get; set; }
    IEnumerable<ColorSelection> ColorSelectionValues { get; }
    ColorSelection ColorSelectionValue { get; set; }
    IEnumerable<Swatch> Swatches { get; }
    ICommand ApplyPrimaryCommand { get; }
    ICommand ApplyAccentCommand { get; }

    string ModuleConfiguration { get; set; }
  }
}
