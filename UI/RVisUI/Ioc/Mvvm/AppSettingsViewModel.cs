using LanguageExt;
using MaterialDesignColors;
using MaterialDesignColors.ColorManipulation;
using ReactiveUI;
using RVis.Base.Extensions;
using RVisUI.AppInf.Design;
using RVisUI.Extensions;
using RVisUI.Ioc.Mvvm;
using RVisUI.Model;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using static System.Globalization.CultureInfo;
using MDPaletteHelper = MaterialDesignThemes.Wpf.PaletteHelper;

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

      View = ReactiveCommand.Create(() => Show = !Show);

      ChangeHue = ReactiveCommand.Create<IHueViewModel>(HandleChangeHue);
      ChangeToPrimary = ReactiveCommand.Create(() => ChangeToScheme(ColorScheme.Primary));
      ChangeToSecondary = ReactiveCommand.Create(() => ChangeToScheme(ColorScheme.Secondary));
      ChangeToPrimaryForeground = ReactiveCommand.Create(() => ChangeToScheme(ColorScheme.PrimaryForeground));
      ChangeToSecondaryForeground = ReactiveCommand.Create(() => ChangeToScheme(ColorScheme.SecondaryForeground));

      SwatchViewModels = SwatchHelper.Swatches
        .Map(s => new SwatchViewModel(s, ChangeHue))
        .ToArr<ISwatchViewModel>();

      var hueViewModel = FindHueViewModelFromAppSettings(ActiveScheme);
      if (hueViewModel is null)
      {
        var theme = _mdPaletteHelper.GetTheme();
        hueViewModel = FindHueViewModelFromHue(theme.PrimaryMid.Color);
      }
      if (hueViewModel is not null) hueViewModel.IsSelected = true;

      SetSecondaryHueHexes();

      _appSettings.PropertyChanged += HandleAppSettingsChanged;
    }

    public AppSettingsViewModel()
      : this(new AppSettings() { PrimaryColorHue = 8, PrimaryColorName = "Indigo" })
    {
    }

    public ICommand View { get; }

    public bool Show
    {
      get => _show;
      set => this.RaiseAndSetIfChanged(ref _show, value);
    }
    private bool _show = false;

    public bool RestoreWindow
    {
      get => _appSettings.RestoreWindow;
      set
      {
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
        _appSettings.RThrottlingUseCores = value + 1;
        this.RaisePropertyChanged();
      }
    }

    public bool IsBaseDark
    {
      get => _appSettings.IsBaseDark;
      set
      {
        _mdPaletteHelper.ChangeBaseTheme(value);
        App.Current.SetLightDark(value);
        _appSettings.IsBaseDark = value;
      }
    }

    public Arr<ISwatchViewModel> SwatchViewModels { get; }

    public ColorScheme ActiveScheme
    {
      get => _activeScheme;
      set => this.RaiseAndSetIfChanged(ref _activeScheme, value);
    }
    private ColorScheme _activeScheme;

    public string? SecondaryHueLightHex
    {
      get => _secondaryHueLightHex;
      set => this.RaiseAndSetIfChanged(ref _secondaryHueLightHex, value);
    }
    private string? _secondaryHueLightHex;

    public string? SecondaryHueMidHex
    {
      get => _secondaryHueMidHex;
      set => this.RaiseAndSetIfChanged(ref _secondaryHueMidHex, value);
    }
    private string? _secondaryHueMidHex;

    public string? SecondaryHueDarkHex
    {
      get => _secondaryHueDarkHex;
      set => this.RaiseAndSetIfChanged(ref _secondaryHueDarkHex, value);
    }
    private string? _secondaryHueDarkHex;

    public string? SecondaryHueMidForegroundHex
    {
      get => _secondaryHueMidForegroundHex;
      set => this.RaiseAndSetIfChanged(ref _secondaryHueMidForegroundHex, value);
    }
    private string? _secondaryHueMidForegroundHex;

    public ICommand ChangeHue { get; }
    public ICommand ChangeToPrimary { get; }
    public ICommand ChangeToSecondary { get; }
    public ICommand ChangeToPrimaryForeground { get; }
    public ICommand ChangeToSecondaryForeground { get; }

    public string ModuleConfiguration
    {
      get => _appSettings.ModuleConfiguration;
      set
      {
        _appSettings.ModuleConfiguration = value;
        this.RaisePropertyChanged();
      }
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

    private IHueViewModel? FindHueViewModelFromHue(Color hue)
    {
      foreach (var swatchViewModel in SwatchViewModels)
      {
        var hueViewModel = swatchViewModel.HueViewModels.SingleOrDefault(hvm => hvm.Hue == hue);
        if (hueViewModel is not null) return hueViewModel;
      }
      return null;
    }

    private IHueViewModel? FindHueViewModelFromAppSettings(ColorScheme colorScheme)
    {
      string? colorName;
      int colorHue;

      switch (colorScheme)
      {
        case ColorScheme.Primary:
          colorName = _appSettings.PrimaryColorName;
          colorHue = _appSettings.PrimaryColorHue;
          break;

        case ColorScheme.PrimaryForeground:
          colorName = _appSettings.PrimaryForegroundColorName;
          colorHue = _appSettings.PrimaryForegroundColorHue;
          break;

        case ColorScheme.Secondary:
          colorName = _appSettings.SecondaryColorName;
          colorHue = _appSettings.SecondaryColorHue;
          break;

        case ColorScheme.SecondaryForeground:
          colorName = _appSettings.SecondaryForegroundColorName;
          colorHue = _appSettings.SecondaryForegroundColorHue;
          break;

        default: throw new ArgumentOutOfRangeException(nameof(colorScheme));
      }

      if (colorName.IsntAString()) return default;

      var swatchViewModel = SwatchViewModels
        .Find(svm => svm.Swatch.Name.EqualsCI(colorName))
        .AssertSome($"Unknown swatch name: {colorName}");

      return swatchViewModel.HueViewModels[colorHue];
    }

    private void HandleChangeHue(IHueViewModel hueViewModel)
    {
      var currentlySelected = SwatchViewModels
        .SelectMany(svm => svm.HueViewModels)
        .SingleOrDefault(hvm => hvm.IsSelected);
      var isUnset = false;

      if (currentlySelected is not null)
      {
        currentlySelected.IsSelected = false;
        isUnset =
          currentlySelected.Swatch.Name == hueViewModel.Swatch.Name &&
          currentlySelected.HueIndex == hueViewModel.HueIndex
          ;
      }

      switch (ActiveScheme)
      {
        case ColorScheme.Primary:
          _mdPaletteHelper.ChangePrimaryColor(hueViewModel.Hue);
          _appSettings.PrimaryColorName = hueViewModel.Swatch.Name;
          _appSettings.PrimaryColorHue = hueViewModel.HueIndex;
          break;

        case ColorScheme.PrimaryForeground:
          if (isUnset)
          {
            _mdPaletteHelper.ChangePrimaryForegroundColor(default);
            _appSettings.PrimaryForegroundColorName = default;
          }
          else
          {
            _mdPaletteHelper.ChangePrimaryForegroundColor(hueViewModel.Hue);
            _appSettings.PrimaryForegroundColorName = hueViewModel.Swatch.Name;
            _appSettings.PrimaryForegroundColorHue = hueViewModel.HueIndex;
          }
          break;

        case ColorScheme.Secondary:
          _mdPaletteHelper.ChangeSecondaryColor(hueViewModel.Hue);
          _appSettings.SecondaryColorName = hueViewModel.Swatch.Name;
          _appSettings.SecondaryColorHue = hueViewModel.HueIndex;
          SetSecondaryHueHexes();
          break;

        case ColorScheme.SecondaryForeground:
          if (isUnset)
          {
            _mdPaletteHelper.ChangeSecondaryForegroundColor(default);
            _appSettings.SecondaryForegroundColorName = default;
          }
          else
          {
            _mdPaletteHelper.ChangeSecondaryForegroundColor(hueViewModel.Hue);
            _appSettings.SecondaryForegroundColorName = hueViewModel.Swatch.Name;
            _appSettings.SecondaryForegroundColorHue = hueViewModel.HueIndex;
          }
          break;
      }

      if (!isUnset) hueViewModel.IsSelected = true;
    }

    private void ChangeToScheme(ColorScheme colorScheme)
    {
      var currentlySelected = SwatchViewModels
        .SelectMany(svm => svm.HueViewModels)
        .SingleOrDefault(hvm => hvm.IsSelected);
      if (currentlySelected is not null) currentlySelected.IsSelected = false;

      ActiveScheme = colorScheme;

      currentlySelected = FindHueViewModelFromAppSettings(ActiveScheme);
      if (currentlySelected is null)
      {
        var theme = _mdPaletteHelper.GetTheme();
        currentlySelected = FindHueViewModelFromHue(ActiveScheme == ColorScheme.Primary
          ? theme.PrimaryMid.Color
          : theme.SecondaryMid.Color
          );
      }
      if (currentlySelected is not null) currentlySelected.IsSelected = true;
    }

    private void SetSecondaryHueHexes()
    {
      static string int2Hex(int i) => i.ToString("X", InvariantCulture).ToLowerInvariant();

      static string colorToHex(Color c) => "#" +
        int2Hex(c.R) +
        int2Hex(c.G) +
        int2Hex(c.B);

      var theme = _mdPaletteHelper.GetTheme();

      var hueViewModel = FindHueViewModelFromAppSettings(ColorScheme.Secondary);
      if (hueViewModel is null)
      {
        hueViewModel = FindHueViewModelFromHue(theme.SecondaryMid.Color);
      }

      Color hue;
      if (hueViewModel is null)
      {
        hue = theme.SecondaryMid.Color;
      }
      else
      {
        hue = hueViewModel!.Hue;
      }

      SecondaryHueLightHex = colorToHex(hue.Lighten());
      SecondaryHueMidHex = colorToHex(hue);
      SecondaryHueDarkHex = colorToHex(hue.Darken());

      hueViewModel = FindHueViewModelFromAppSettings(ColorScheme.SecondaryForeground);
      if (hueViewModel is null && theme.SecondaryMid.ForegroundColor is not null)
      {
        hueViewModel = FindHueViewModelFromHue(theme.SecondaryMid.ForegroundColor.Value);
      }

      if (hueViewModel is null)
      {
        hue = theme.SecondaryMid.ForegroundColor ?? hue.ContrastingForegroundColor();
      }
      else
      {
        hue = hueViewModel.Hue;
      }

      SecondaryHueMidForegroundHex = colorToHex(hue);
    }

    private readonly MDPaletteHelper _mdPaletteHelper = new();
    private readonly IAppSettings _appSettings;
  }
}
