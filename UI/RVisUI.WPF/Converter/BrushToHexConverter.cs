﻿using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

#nullable disable

namespace RVisUI.Wpf
{
  public class BrushToHexConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value == null) return null;

      static string lowerHexString(int i) => i.ToString("X").ToLower();

      var brush = (SolidColorBrush)value;

      var hex = lowerHexString(brush.Color.R) +
                lowerHexString(brush.Color.G) +
                lowerHexString(brush.Color.B);

      return "#" + hex;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
