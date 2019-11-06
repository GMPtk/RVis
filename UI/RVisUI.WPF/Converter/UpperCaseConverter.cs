using System;
using System.Globalization;
using System.Windows.Data;

namespace RVisUI.Wpf
{
  public class UpperCaseConverter : IValueConverter
  {
    public static readonly UpperCaseConverter Default = new UpperCaseConverter();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (!(value is string s)) return value;
      return s.ToUpper(culture);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
