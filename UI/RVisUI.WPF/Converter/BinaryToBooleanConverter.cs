using System;
using System.Globalization;
using System.Windows.Data;

#nullable disable

namespace RVisUI.Wpf
{
  public class BinaryToBooleanConverter : IValueConverter
  {
    public Type Type { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is double d) return d != 0d;

      if (value is int i) return i != 0;

      return default;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is not bool b) return default;

      if (Type == typeof(int)) return b ? 1 : 0;

      return b ? 1d : 0d;
    }
  }
}
