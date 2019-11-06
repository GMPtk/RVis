using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using static RVis.Base.Check;

namespace RVisUI.Wpf
{
  public class PixelToGridLengthConverter : IValueConverter
  {
    public static readonly PixelToGridLengthConverter Default = new PixelToGridLengthConverter();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      var pixels = RequireInstanceOf<double>(value);
      return new GridLength(pixels);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      var gridLength = RequireInstanceOf<GridLength>(value);
      RequireTrue(gridLength.IsAbsolute);
      return gridLength.Value;
    }
  }
}
