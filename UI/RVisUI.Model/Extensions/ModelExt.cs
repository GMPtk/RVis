namespace RVisUI.Model.Extensions
{
  public static class ModelExt
  {
    public static bool IsThemeProperty(this string? propertyName)
    {
      return propertyName switch
      {
        nameof(IAppSettings.PrimaryColorName) or
        nameof(IAppSettings.SecondaryColorName) or
        nameof(IAppSettings.IsBaseDark) or
        nameof(IAppSettings.IsColorAdjusted) or
        nameof(IAppSettings.DesiredContrastRatio) or
        nameof(IAppSettings.ContrastValue) or
        nameof(IAppSettings.ColorSelectionValue) => true,

        _ => false,
      };
    }
  }
}
