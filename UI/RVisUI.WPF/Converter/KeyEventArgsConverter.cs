using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;

#nullable disable

namespace RVisUI.Wpf
{
  public class KeyEventArgsConverter : IValueConverter
  {
    public static readonly KeyEventArgsConverter Default = new KeyEventArgsConverter();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is not KeyEventArgs keyEventArgs) return default;

      return (
        keyEventArgs.Key, 
        keyEventArgs.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control), 
        keyEventArgs.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Shift)
        );
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
