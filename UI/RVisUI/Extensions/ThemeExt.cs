using MaterialDesignColors;
using MaterialDesignColors.ColorManipulation;
using MaterialDesignThemes.Wpf;
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
  }
}
