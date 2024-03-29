﻿using RVisUI.Model;
using RVisUI.Properties;
using System.ComponentModel;
using System.Configuration;
using System.Runtime.CompilerServices;
using System.Linq;

namespace RVisUI.Ioc
{
  internal sealed class AppSettings : IAppSettings
  {
    public AppSettings()
    {
    }

    public bool RestoreWindow
    {
      get => Settings.Default.RestoreWindow;
      set
      {
        if (value != Settings.Default.RestoreWindow)
        {
          Settings.Default.RestoreWindow = value;
          Settings.Default.Save();
          NotifyPropertyChanged();
        }
      }
    }

    public string PrimaryColorName
    {
      get => Settings.Default.PrimaryColorName;
      set
      {
        if (value != Settings.Default.PrimaryColorName)
        {
          Settings.Default.PrimaryColorName = value;
          Settings.Default.Save();
          NotifyPropertyChanged();
        }
      }
    }

    public string SecondaryColorName
    {
      get => Settings.Default.SecondaryColorName;
      set
      {
        if (value != Settings.Default.SecondaryColorName)
        {
          Settings.Default.SecondaryColorName = value;
          Settings.Default.Save();
          NotifyPropertyChanged();
        }
      }
    }

    public bool IsBaseDark
    {
      get => Settings.Default.IsBaseDark;
      set
      {
        if (value != Settings.Default.IsBaseDark)
        {
          Settings.Default.IsBaseDark = value;
          Settings.Default.Save();
          NotifyPropertyChanged();
        }
      }
    }

    public bool IsColorAdjusted 
    {
      get => Settings.Default.IsColorAdjusted;
      set
      {
        if (value != Settings.Default.IsColorAdjusted)
        {
          Settings.Default.IsColorAdjusted = value;
          Settings.Default.Save();
          NotifyPropertyChanged();
        }
      }
    }

    public float DesiredContrastRatio 
    {
      get => Settings.Default.DesiredContrastRatio;
      set
      {
        if (value != Settings.Default.DesiredContrastRatio)
        {
          Settings.Default.DesiredContrastRatio = value;
          Settings.Default.Save();
          NotifyPropertyChanged();
        }
      }
    }

    public int ContrastValue 
    {
      get => Settings.Default.ContrastValue;
      set
      {
        if (value != Settings.Default.ContrastValue)
        {
          Settings.Default.ContrastValue = value;
          Settings.Default.Save();
          NotifyPropertyChanged();
        }
      }
    }

    public int ColorSelectionValue 
    {
      get => Settings.Default.ColorSelectionValue;
      set
      {
        if (value != Settings.Default.ColorSelectionValue)
        {
          Settings.Default.ColorSelectionValue = value;
          Settings.Default.Save();
          NotifyPropertyChanged();
        }
      }
    }

    public string ModuleConfiguration
    {
      get => Settings.Default.ModuleConfiguration;
      set
      {
        if (value != Settings.Default.ModuleConfiguration)
        {
          Settings.Default.ModuleConfiguration = value;
          Settings.Default.Save();
          NotifyPropertyChanged();
        }
      }
    }

    public int RThrottlingUseCores
    {
      get => Settings.Default.RThrottlingUseCores;
      set
      {
        if (value != Settings.Default.RThrottlingUseCores)
        {
          Settings.Default.RThrottlingUseCores = value;
          Settings.Default.Save();
          NotifyPropertyChanged();
        }
      }
    }

    public string PathToSimLibrary
    {
      get => Settings.Default.PathToSimLibrary;
      set
      {
        if (value != Settings.Default.PathToSimLibrary)
        {
          Settings.Default.PathToSimLibrary = value;
          Settings.Default.Save();
          NotifyPropertyChanged();
        }
      }
    }

    public string PathToRunControlDrop
    {
      get => Settings.Default.PathToRunControlDrop;
      set
      {
        if (value != Settings.Default.PathToRunControlDrop)
        {
          Settings.Default.PathToRunControlDrop = value;
          Settings.Default.Save();
          NotifyPropertyChanged();
        }
      }
    }

    public double Zoom
    {
      get => Settings.Default.Zoom;
      set
      {
        if (value != Settings.Default.Zoom)
        {
          Settings.Default.Zoom = value;
          Settings.Default.Save();
          NotifyPropertyChanged();
        }
      }
    }

    public T Get<T>(string name)
    {
      ApplicationSettingsBase applicationSettingsBase = Settings.Default;
      EnsureProperty<T>(applicationSettingsBase, name);
      var t = applicationSettingsBase[name];
      return (T)t;
    }

    public void Set<T>(string name, T t)
    {
      ApplicationSettingsBase applicationSettingsBase = Settings.Default;
      EnsureProperty<T>(applicationSettingsBase, name);
      applicationSettingsBase[name] = t;
      applicationSettingsBase.Save();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") => 
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private static void EnsureProperty<T>(ApplicationSettingsBase applicationSettingsBase, string name)
    {
      if (!applicationSettingsBase.Properties.OfType<SettingsProperty>().Any(sp => sp.Name == name))
      {
        SettingsProvider settingsProvider = applicationSettingsBase.Providers["LocalFileSettingsProvider"];
        var settingsProperty = new SettingsProperty(name)
        {
          PropertyType = typeof(T),
          Provider = settingsProvider,
          SerializeAs = SettingsSerializeAs.Xml
        };
        settingsProperty.Attributes.Add(typeof(UserScopedSettingAttribute), new UserScopedSettingAttribute());
        applicationSettingsBase.Properties.Add(settingsProperty);
        applicationSettingsBase.Reload();
      }
    }
  }
}
