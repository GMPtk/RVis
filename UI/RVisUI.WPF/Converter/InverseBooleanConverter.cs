using System;
using System.Globalization;
using System.Windows.Data;
using static RVis.Base.Check;

namespace RVisUI.Wpf
{
  public sealed class InverseBooleanConverter : IValueConverter
  {
    public static readonly InverseBooleanConverter Default = new InverseBooleanConverter();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      RequireTrue(targetType == typeof(bool));
      return !(bool)value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
      throw new NotSupportedException();
  }
}
