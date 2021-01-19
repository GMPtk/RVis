namespace RVisUI.Model.Extensions
{
  public static class ModelExt
  {
    public static string GetAppThemeName(this IAppSettings appSettings) =>
      appSettings.IsBaseDark ? "BaseDark" : "BaseLight";

    public static bool IsThemeProperty(this string? propertyName)
    {
      return propertyName switch
      {
        nameof(IAppSettings.IsBaseDark) or
        nameof(IAppSettings.PrimaryColorName) or
        nameof(IAppSettings.PrimaryColorHue) or
        nameof(IAppSettings.SecondaryColorName) or
        nameof(IAppSettings.SecondaryColorHue) or
        nameof(IAppSettings.PrimaryForegroundColorName) or
        nameof(IAppSettings.PrimaryForegroundColorHue) or
        nameof(IAppSettings.SecondaryForegroundColorName) or
        nameof(IAppSettings.SecondaryForegroundColorHue) => true,

        _ => false,
      };
    }
  }
}
