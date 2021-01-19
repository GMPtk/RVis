using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

#nullable disable

namespace RVisUI.Wpf
{
  public class EnumMatchToBooleanConverter : IValueConverter
  {
    public static readonly EnumMatchToBooleanConverter Default = new EnumMatchToBooleanConverter();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value == null || parameter == null) return false;

      var checkValue = value.ToString();
      var targetValue = parameter.ToString();

      return checkValue.Equals(targetValue, StringComparison.InvariantCultureIgnoreCase);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is not bool b || !b) return null;

      if (parameter == null) return null;

      return Enum.Parse(targetType, parameter.ToString());
    }
  }
}
