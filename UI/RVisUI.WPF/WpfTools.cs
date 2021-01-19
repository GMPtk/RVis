using System.ComponentModel;
using System.Windows;

namespace RVisUI.Wpf
{
  public static class WpfTools
  {
    public static bool IsInDesignMode =>
      DesignerProperties.GetIsInDesignMode(new DependencyObject());
  }
}
