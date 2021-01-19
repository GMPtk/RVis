using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

#nullable disable

// https://stackoverflow.com/questions/534575/how-do-i-invert-booleantovisibilityconverter

namespace RVisUI.Wpf
{
  public sealed class BooleanToVisibilityConverter : BooleanConverter<Visibility>
  {
    public BooleanToVisibilityConverter() :
      base(Visibility.Visible, Visibility.Collapsed)
    {
    }
  }

  public abstract class BooleanConverter<T> : IValueConverter
  {
    public BooleanConverter(T trueValue, T falseValue)
    {
      True = trueValue;
      False = falseValue;
    }

    public T True { get; set; }
    public T False { get; set; }

    public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
      value is bool b && b ? True : False;

    public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
      value is T t && EqualityComparer<T>.Default.Equals(t, True);
  }
}
