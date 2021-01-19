using RVis.Base.Extensions;
using System;
using System.Globalization;
using System.Windows.Data;

#nullable disable

namespace RVisUI.Wpf
{
  public class SigFigsConverter : IValueConverter
  {
    public static readonly SigFigsConverter Default = new SigFigsConverter();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is not double d) return default;

      if (parameter is not int digits) return default;

      return d.ToSigFigs(digits);
    }
  }
}
