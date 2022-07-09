using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

namespace RVisUI
{
  public partial class App
  {
    //public void SetLightDark(bool isDark)
    //{
    //  // this method is a copy of MaterialDesignThemes.Wpf.PaletteHelper.SetLightDark(bool isDark) with changed resource names/strings

    //  // MaterialDesignExtensions
    //  var oldThemeResourceDictionary = Resources.MergedDictionaries
    //    .Where(resourceDictionary => resourceDictionary != null && resourceDictionary.Source != null)
    //    .SingleOrDefault(resourceDictionary => Regex.Match(resourceDictionary.Source.OriginalString, @"(\/MaterialDesignExtensions;component\/Themes\/MaterialDesign)((Light)|(Dark))Theme\.").Success);

    //  if (oldThemeResourceDictionary == null)
    //  {
    //    throw new ApplicationException($"Unable to find light or dark theme in application resources.");
    //  }

    //  string newThemeSource = $"pack://application:,,,/MaterialDesignExtensions;component/Themes/MaterialDesign{(isDark ? "Dark" : "Light")}Theme.xaml";
    //  var newThemeResourceDictionary = new ResourceDictionary() { Source = new Uri(newThemeSource) };

    //  Resources.MergedDictionaries.Remove(oldThemeResourceDictionary);
    //  Resources.MergedDictionaries.Add(newThemeResourceDictionary);

    //  // MahApps
    //  var oldMahAppsResourceDictionary = Resources.MergedDictionaries
    //    .Where(resourceDictionary => resourceDictionary != null && resourceDictionary.Source != null)
    //    .SingleOrDefault(resourceDictionary => Regex.Match(resourceDictionary.Source.OriginalString, @"(\/MahApps.Metro;component\/Styles\/Accents\/)((BaseLight)|(BaseDark))").Success);

    //  if (oldMahAppsResourceDictionary != null)
    //  {
    //    newThemeSource = $"pack://application:,,,/MahApps.Metro;component/Styles/Accents/{(isDark ? "BaseDark" : "BaseLight")}.xaml";
    //    var newMahAppsResourceDictionary = new ResourceDictionary { Source = new Uri(newThemeSource) };

    //    Resources.MergedDictionaries.Remove(oldMahAppsResourceDictionary);
    //    Resources.MergedDictionaries.Add(newMahAppsResourceDictionary);
    //  }
    //}
  }
}
