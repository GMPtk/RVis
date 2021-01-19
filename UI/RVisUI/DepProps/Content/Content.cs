using LanguageExt;
using MaterialDesignThemes.Wpf;
using RVis.Base.Extensions;
using RVisUI.Model;
using System;
using System.Windows;
using System.Windows.Controls;

namespace RVisUI.DepProps
{
  public static class Content
  {
    public static string? GetUIComponents(DependencyObject o)
    {
      return o.GetValue(UIComponentsProperty) as string;
    }

    public static void SetUIComponents(DependencyObject o, string value)
    {
      o.SetValue(UIComponentsProperty, value);
    }

    public static readonly DependencyProperty UIComponentsProperty =
      DependencyProperty.RegisterAttached(
        nameof(IAppState.UIComponents),
        typeof(Arr<(string ID, string DisplayName, string DisplayIcon, object View, object ViewModel)>),
        typeof(Content),
        new UIPropertyMetadata(default(Arr<(string ID, string DisplayName, string DisplayIcon, object View, object ViewModel)>), HandleUIComponentsChanged)
        );

    private static void HandleUIComponentsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (!d.Resolve(out TabControl? tabControl)) return;

      if (!e.OldValue.Resolve(out Arr<(string ID, string DisplayName, string DisplayIcon, object View, object ViewModel)> oldUIComponents)) return;

      var isStaleControl = !tabControl.IsLoaded && !oldUIComponents.IsEmpty;
      if (isStaleControl) return;

      if (!e.NewValue.Resolve(out Arr<(string ID, string DisplayName, string DisplayIcon, object View, object ViewModel)> newUIComponents)) return;

      tabControl.Items.Clear();

      var tabItems = newUIComponents.Map(c =>
      {
        object header = c.DisplayName.ToUpperInvariant();
        string? toolTip = default;

        if (c.DisplayIcon != default)
        {
          try
          {
            var kind = (PackIconKind)Enum.Parse(typeof(PackIconKind), c.DisplayIcon, true);
            header = new PackIcon() { Kind = kind, Width = 25, Height = 25 };
            toolTip = c.DisplayName;
          }
          catch (Exception ex)
          {
            App.Current.Log.Error(ex);
          }
        }

        var tabItem = new TabItem
        {
          Header = header,
          ToolTip = toolTip,
          Content = c.View,
          DataContext = c.ViewModel,
          Tag = c.ID
        };

        return tabItem;
      });

      tabItems.Iter(ti => tabControl.Items.Add(ti));
    }
  }
}
