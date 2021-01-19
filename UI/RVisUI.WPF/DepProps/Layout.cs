using RVis.Base.Extensions;
using System;
using System.Windows;
using System.Windows.Controls;

namespace RVisUI.Wpf
{
  public static class Layout
  {
    public static bool? GetIsHidden(DependencyObject o) => 
      (bool?)o.GetValue(IsHiddenProperty);

    public static void SetIsHidden(DependencyObject o, bool? value) => 
      o.SetValue(IsHiddenProperty, value);

    public static readonly DependencyProperty IsHiddenProperty = 
      DependencyProperty.RegisterAttached(
         "IsHidden", 
         typeof(bool?), 
         typeof(Layout), 
         new PropertyMetadata(default(bool?), HandleIsHiddenChanged)
        );

    private static void HandleIsHiddenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (!d.Resolve(out FrameworkElement? frameworkElement)) return;
      var isHidden = (bool?)e.NewValue == true;
      frameworkElement.Visibility = isHidden ? Visibility.Hidden : Visibility.Visible;
    }

    public static Thickness GetMargin(DependencyObject o) => 
      (Thickness)o.GetValue(MarginProperty);

    public static void SetMargin(DependencyObject o, Thickness value) => 
      o.SetValue(MarginProperty, value);

    public static readonly DependencyProperty MarginProperty =
      DependencyProperty.RegisterAttached(
        "Margin",
        typeof(Thickness),
        typeof(Layout),
        new UIPropertyMetadata(new Thickness(), HandleMarginChanged)
        );

    private static void HandleMarginChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is Panel panel)
      {
        panel.Loaded += new RoutedEventHandler(HandlePanelLoaded);
      }
      else if (d is Grid grid)
      {
        grid.Loaded += new RoutedEventHandler(HandlePanelLoaded);
      }
    }

    private static void HandlePanelLoaded(object sender, RoutedEventArgs e)
    {
      UIElementCollection? children = default;

      if (sender is Panel panel)
      {
        children = panel.Children;
      }
      else if (sender is Grid grid)
      {
        children = grid.Children;
      }

      if (children != default)
      {
        foreach (var child in children)
        {
          if (!child.Resolve(out FrameworkElement? frameworkElement)) continue;
          frameworkElement.Margin = GetMargin((DependencyObject)sender);
        }
      }
    }
  }
}
