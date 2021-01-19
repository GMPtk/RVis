using System;
using System.Globalization;
using System.Windows.Data;

#nullable disable

namespace RVisUI.Wpf
{
  public class ElementTypeConverter : IValueConverter
  {
    public static readonly ElementTypeConverter Default = new ElementTypeConverter();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (!targetType.IsArray) throw new ArgumentException("expecting array", nameof(targetType));
      if (value == default) return default;
      if (value is not Array array) throw new ArgumentException("expecting array", nameof(value));
      return CreateArray(array, targetType.GetElementType());
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (!targetType.IsArray) throw new ArgumentException("expecting array", nameof(targetType));
      if (value == default) return default;
      if (value is not Array array) throw new ArgumentException("expecting array", nameof(value));
      return CreateArray(array, targetType.GetElementType());
    }

    private static object CreateArray(Array source, Type elementType)
    {
      var destination = Array.CreateInstance(
        elementType,
        source.Length
        );
      Array.Copy(source, destination, source.Length);
      return destination;
    }
  }
}
