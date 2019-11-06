namespace RVisUI.Model.Extensions
{
  public static class ModelExt
  {
    public static string GetAppThemeName(this IAppSettings appSettings) =>
      appSettings.IsBaseDark ? "BaseDark" : "BaseLight";

    public static bool IsThemeProperty(this string propertyName)
    {
      switch (propertyName)
      {
        case nameof(IAppSettings.IsBaseDark):
        case nameof(IAppSettings.PrimaryColorName):
        case nameof(IAppSettings.PrimaryColorHue):
        case nameof(IAppSettings.SecondaryColorName):
        case nameof(IAppSettings.SecondaryColorHue):
        case nameof(IAppSettings.PrimaryForegroundColorName):
        case nameof(IAppSettings.PrimaryForegroundColorHue):
        case nameof(IAppSettings.SecondaryForegroundColorName):
        case nameof(IAppSettings.SecondaryForegroundColorHue):
          return true;
      }

      return false;
    }
  }
}
