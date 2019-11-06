using MaterialDesignThemes.Wpf;
using System.Windows;

namespace RVisUI.DepProps
{
  public static class Layout
  {
    public static void SetUniformCornerRadius(DependencyObject element, double value) => 
      element.SetValue(UniformCornerRadiusProperty, value);

    public static double GetUniformCornerRadius(DependencyObject element) => 
      (double)element.GetValue(UniformCornerRadiusProperty);

    public static readonly DependencyProperty UniformCornerRadiusProperty =
      DependencyProperty.RegisterAttached(
        "UniformCornerRadius", 
        typeof(double), 
        typeof(Layout), 
        new PropertyMetadata(2.0, OnUniformCornerRadius)
        );

    private static void OnUniformCornerRadius(DependencyObject d, DependencyPropertyChangedEventArgs e) => 
      ButtonAssist.SetCornerRadius(d, new CornerRadius((double)e.NewValue));
  }
}
