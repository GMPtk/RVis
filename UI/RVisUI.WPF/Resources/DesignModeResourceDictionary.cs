using System;
using System.ComponentModel;
using System.Windows;

namespace RVisUI.Wpf
{
  // from http://social.technet.microsoft.com/wiki/contents/articles/23287.trick-to-use-a-resourcedictionary-only-when-in-design-mode.aspx
  public class DesignModeResourceDictionary : ResourceDictionary
  {
    public string? DesignModeSource
    {
      get => _designModeSource;
      set
      {
        _designModeSource = value;
        var isInDesignMode = (bool)DesignerProperties.IsInDesignModeProperty.GetMetadata(typeof(DependencyObject)).DefaultValue;
        if (isInDesignMode && _designModeSource is not null) Source = new Uri(_designModeSource);
      }
    }

    private string? _designModeSource;
  }
}
