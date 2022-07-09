using LanguageExt;
using MaterialDesignColors;
using MaterialDesignThemes.Wpf;
using ReactiveUI;
using RVisUI.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using static System.Globalization.CultureInfo;

namespace RVisUI.Ioc.Mvvm
{
  public sealed class AppSettingsViewModel : ReactiveObject, IAppSettingsViewModel
  {
    public AppSettingsViewModel(IAppSettings appSettings)
    {
      _appSettings = appSettings;

      var nCores = Math.Max(Environment.ProcessorCount - 1, 1);
      CoresOptions = Enumerable
        .Range(1, nCores)
        .Select(c => c.ToString(InvariantCulture))
        .ToArr();

      Swatches = new SwatchesProvider().Swatches;

      ApplyPrimaryCommand = ReactiveCommand.Create<Swatch, Unit>(HandleApplyPrimary);
      ApplyAccentCommand = ReactiveCommand.Create<Swatch, Unit>(HandleApplyAccent);

      _appSettings.PropertyChanged += HandleAppSettingsChanged;
    }

    public AppSettingsViewModel()
      : this(new AppSettings() { PrimaryColorName = "Indigo" })
    {
    }

    public bool RestoreWindow
    {
      get => _appSettings.RestoreWindow;
      set
      {
        if (value == _appSettings.RestoreWindow) return;
        _appSettings.RestoreWindow = value;
        this.RaisePropertyChanged();
      }
    }

    public Arr<string> CoresOptions { get; }

    public int NumberOfCoresSelectedIndex
    {
      get => _appSettings.RThrottlingUseCores - 1;
      set
      {
        if (value == _appSettings.RThrottlingUseCores - 1) return;
        _appSettings.RThrottlingUseCores = value + 1;
        this.RaisePropertyChanged();
      }
    }

    public bool IsBaseDark
    {
      get => _appSettings.IsBaseDark;
      set
      {
        if (value == _appSettings.IsBaseDark) return;
        ModifyTheme(theme => theme.SetBaseTheme(value ? Theme.Dark : Theme.Light));
        _appSettings.IsBaseDark = value;
        this.RaisePropertyChanged();
      }
    }

    public bool IsColorAdjusted
    {
      get => _appSettings.IsColorAdjusted;
      set
      {
        if (value == _appSettings.IsColorAdjusted) return;
        ModifyTheme(theme =>
        {
          if (theme is Theme internalTheme)
          {
            internalTheme.ColorAdjustment = value
              ? new ColorAdjustment
              {
                DesiredContrastRatio = DesiredContrastRatio,
                Contrast = ContrastValue,
                Colors = ColorSelectionValue
              }
              : null;
          }
        });
        _appSettings.IsColorAdjusted = value;
        this.RaisePropertyChanged();
      }
    }

    public float DesiredContrastRatio
    {
      get => _appSettings.DesiredContrastRatio;
      set
      {
        if (value == _appSettings.DesiredContrastRatio) return;
        ModifyTheme(theme =>
        {
          if (theme is Theme internalTheme && internalTheme.ColorAdjustment != null)
          {
            internalTheme.ColorAdjustment.DesiredContrastRatio = value;
          }
        });
        _appSettings.DesiredContrastRatio = value;
        this.RaisePropertyChanged();
      }
    }

    public IEnumerable<Contrast> ContrastValues => Enum.GetValues(typeof(Contrast)).Cast<Contrast>();

    public Contrast ContrastValue
    {
      get => (Contrast)_appSettings.ContrastValue;
      set
      {
        if ((int)value == _appSettings.ContrastValue) return;
        ModifyTheme(theme =>
        {
          if (theme is Theme internalTheme && internalTheme.ColorAdjustment != null)
          {
            internalTheme.ColorAdjustment.Contrast = value;
          }
        });
        _appSettings.ContrastValue = (int)value;
        this.RaisePropertyChanged();
      }
    }

    public IEnumerable<ColorSelection> ColorSelectionValues => Enum.GetValues(typeof(ColorSelection)).Cast<ColorSelection>();

    public ColorSelection ColorSelectionValue
    {
      get => (ColorSelection)_appSettings.ColorSelectionValue;
      set
      {
        if ((int)value == _appSettings.ColorSelectionValue) return;
        ModifyTheme(theme =>
        {
          if (theme is Theme internalTheme && internalTheme.ColorAdjustment != null)
          {
            internalTheme.ColorAdjustment.Colors = value;
          }
        });
        _appSettings.ColorSelectionValue = (int)value;
        this.RaisePropertyChanged();
      }
    }

    public IEnumerable<Swatch> Swatches { get; }

    public ICommand ApplyPrimaryCommand { get; }

    public ICommand ApplyAccentCommand { get; }

    public string ModuleConfiguration
    {
      get => _appSettings.ModuleConfiguration;
      set
      {
        _appSettings.ModuleConfiguration = value;
        this.RaisePropertyChanged();
      }
    }

    private Unit HandleApplyPrimary(Swatch swatch)
    {
      ModifyTheme(theme => theme.SetPrimaryColor(swatch.ExemplarHue.Color));
      _appSettings.PrimaryColorName = swatch.Name;
      return Unit.Default;
    }

    private Unit HandleApplyAccent(Swatch swatch)
    {
      if (swatch is { AccentExemplarHue: not null })
      {
        ModifyTheme(theme => theme.SetSecondaryColor(swatch.AccentExemplarHue.Color));
        _appSettings.SecondaryColorName = swatch.Name;
      }

      return Unit.Default;
    }

    private void HandleAppSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
      switch (e.PropertyName)
      {
        case nameof(IAppSettings.RestoreWindow):
        case nameof(IAppSettings.IsBaseDark):
        case nameof(IAppSettings.ModuleConfiguration):
          this.RaisePropertyChanged(e.PropertyName);
          break;
      }
    }

    private static void ModifyTheme(Action<ITheme> modificationAction)
    {
      var paletteHelper = new PaletteHelper();
      var theme = paletteHelper.GetTheme();
      modificationAction?.Invoke(theme);
      paletteHelper.SetTheme(theme);
    }

    private readonly IAppSettings _appSettings;
  }
}
