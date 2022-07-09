using MaterialDesignColors;
using MaterialDesignThemes.Wpf;
using RVis.Base.Extensions;
using RVisUI.Properties;
using System.Linq;

namespace RVisUI.Extensions
{
  internal static class SettingsExt
  {
    internal static void ApplyTheme(this Settings settings)
    {
      var colorAdjustment = new ColorAdjustment();

      var isCustomized =
        settings.PrimaryColorName.IsAString() ||
        settings.SecondaryColorName.IsAString() ||
        settings.IsBaseDark != false ||
        settings.IsColorAdjusted != false ||
        settings.DesiredContrastRatio != colorAdjustment.DesiredContrastRatio ||
        settings.ContrastValue != (int)colorAdjustment.Contrast ||
        settings.ColorSelectionValue != (int)colorAdjustment.Colors;

      if (!isCustomized)
      {
        return;
      }

      var paletteHelper = new PaletteHelper();
      var theme = paletteHelper.GetTheme();

      theme.SetBaseTheme(settings.IsBaseDark ? Theme.Dark : Theme.Light);

      var swatches = new SwatchesProvider().Swatches;

      if (settings.PrimaryColorName.IsAString())
      {
        var swatch = swatches.Single(s => s.Name == settings.PrimaryColorName);
        theme.SetPrimaryColor(swatch.ExemplarHue.Color);
      }

      if (settings.SecondaryColorName.IsAString())
      {
        var swatch = swatches.Single(s => s.Name == settings.SecondaryColorName);
        theme.SetSecondaryColor(swatch.ExemplarHue.Color);
      }

      if (settings.IsColorAdjusted && theme is Theme t)
      {
        colorAdjustment = new ColorAdjustment
        {
          DesiredContrastRatio = settings.DesiredContrastRatio,
          Contrast = (Contrast)settings.ContrastValue,
          Colors = (ColorSelection)settings.ColorSelectionValue
        };
        t.ColorAdjustment = colorAdjustment;
      }

      paletteHelper.SetTheme(theme);
    }
  }
}
