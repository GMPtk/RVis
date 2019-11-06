using MaterialDesignColors;
using MaterialDesignColors.ColorManipulation;
using MaterialDesignThemes.Wpf;
using RVis.Base.Extensions;
using RVisUI.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace RVisUI.Extensions
{
  internal static class ThemeExt
  {
    internal static void ChangeBaseTheme(this PaletteHelper paletteHelper, bool isDark)
    {
      var theme = paletteHelper.GetTheme();
      var baseTheme = isDark ? new MaterialDesignDarkTheme() : (IBaseTheme)new MaterialDesignLightTheme();
      theme.SetBaseTheme(baseTheme);
      paletteHelper.SetTheme(theme);
    }

    public static void ChangePrimaryColor(this PaletteHelper paletteHelper, Color color)
    {
      var theme = paletteHelper.GetTheme();

      theme.PrimaryLight = new ColorPair(color.Lighten(), theme.PrimaryLight.ForegroundColor);
      theme.PrimaryMid = new ColorPair(color, theme.PrimaryMid.ForegroundColor);
      theme.PrimaryDark = new ColorPair(color.Darken(), theme.PrimaryDark.ForegroundColor);

      paletteHelper.SetTheme(theme);
    }

    public static void ChangePrimaryForegroundColor(this PaletteHelper paletteHelper, Color? color)
    {
      var theme = paletteHelper.GetTheme();

      theme.PrimaryLight = new ColorPair(theme.PrimaryLight.Color, color?.Lighten());
      theme.PrimaryMid = new ColorPair(theme.PrimaryMid.Color, color);
      theme.PrimaryDark = new ColorPair(theme.PrimaryDark.Color, color?.Darken());

      paletteHelper.SetTheme(theme);
    }

    public static void ChangeSecondaryColor(this PaletteHelper paletteHelper, Color color)
    {
      var theme = paletteHelper.GetTheme();

      theme.SecondaryLight = new ColorPair(color.Lighten(), theme.SecondaryLight.ForegroundColor);
      theme.SecondaryMid = new ColorPair(color, theme.SecondaryMid.ForegroundColor);
      theme.SecondaryDark = new ColorPair(color.Darken(), theme.SecondaryDark.ForegroundColor);

      paletteHelper.SetTheme(theme);
    }

    public static void ChangeSecondaryForegroundColor(this PaletteHelper paletteHelper, Color? color)
    {
      var theme = paletteHelper.GetTheme();

      theme.SecondaryLight = new ColorPair(theme.SecondaryLight.Color, color?.Lighten());
      theme.SecondaryMid = new ColorPair(theme.SecondaryMid.Color, color);
      theme.SecondaryDark = new ColorPair(theme.SecondaryDark.Color, color?.Darken());

      paletteHelper.SetTheme(theme);
    }

    internal static void ApplyTheme(this Settings settings) => 
      ApplyTheme(
        settings.IsBaseDark,
        settings.PrimaryColorName,
        settings.PrimaryColorHue,
        settings.SecondaryColorName,
        settings.SecondaryColorHue,
        settings.PrimaryForegroundColorName,
        settings.PrimaryForegroundColorHue,
        settings.SecondaryForegroundColorName,
        settings.SecondaryForegroundColorHue
        );

    internal static void ApplyTheme(
      bool isBaseDark, 
      string primaryColorName, 
      int primaryColorHue,
      string secondaryColorName,
      int secondaryColorHue,
      string primaryForegroundColorName,
      int primaryForegroundColorHue,
      string secondaryForegroundColorName,
      int secondaryForegroundColorHue
      )
    {
      var paletteHelper = new PaletteHelper();
      var theme = paletteHelper.GetTheme();
      var baseTheme = isBaseDark ? new MaterialDesignDarkTheme() : (IBaseTheme)new MaterialDesignLightTheme();
      theme.SetBaseTheme(baseTheme);

      var swatch = SwatchHelper.Swatches.SingleOrDefault(s => s.Name.EqualsCI(primaryColorName));
      var primaryColor = swatch == default ? theme.PrimaryMid.Color : swatch.Hues.ToArray()[primaryColorHue];

      swatch = SwatchHelper.Swatches.SingleOrDefault(s => s.Name.EqualsCI(primaryForegroundColorName));
      var primaryForegroundColor = swatch == default ? theme.PrimaryMid.ForegroundColor : swatch.Hues.ToArray()[primaryForegroundColorHue];

      swatch = SwatchHelper.Swatches.SingleOrDefault(s => s.Name.EqualsCI(secondaryColorName));
      var secondaryColor = swatch == default ? theme.SecondaryMid.Color : swatch.Hues.ToArray()[secondaryColorHue];

      swatch = SwatchHelper.Swatches.SingleOrDefault(s => s.Name.EqualsCI(secondaryForegroundColorName));
      var secondaryForegroundColor = swatch == default ? theme.SecondaryMid.ForegroundColor : swatch.Hues.ToArray()[secondaryForegroundColorHue];

      theme.PrimaryLight = new ColorPair(primaryColor.Lighten(), primaryForegroundColor?.Lighten());
      theme.PrimaryMid = new ColorPair(primaryColor, primaryForegroundColor);
      theme.PrimaryDark = new ColorPair(primaryColor.Darken(), primaryForegroundColor?.Darken());

      theme.SecondaryLight = new ColorPair(secondaryColor.Lighten(), secondaryForegroundColor?.Lighten());
      theme.SecondaryMid = new ColorPair(secondaryColor, secondaryForegroundColor);
      theme.SecondaryDark = new ColorPair(secondaryColor.Darken(), secondaryForegroundColor?.Darken());

      paletteHelper.SetTheme(theme);
    }
  }
}
