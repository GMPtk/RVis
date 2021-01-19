using LanguageExt;
using MaterialDesignColors;
using MaterialDesignColors.ColorManipulation;
using MaterialDesignThemes.Wpf;
using ReactiveUI;
using RVis.Base.Extensions;
using RVisUI.Extensions;
using RVisUI.Model;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using static RVis.Base.Check;
using static RVisUI.Wpf.WpfTools;
using static System.Globalization.CultureInfo;

namespace RVisUI.Ioc.Mvvm
{
  internal sealed class AppSettingsViewModel : ReactiveObject, IAppSettingsViewModel
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

      var hueViewModel = FindHueViewModel(ActiveScheme, SwatchViewModels, _appSettings).AssertNotNull();

      hueViewModel.IsSelected = true;

      SetSecondaryHueHexes();

      _appSettings.PropertyChanged += HandleAppSettingsChanged;
    }

    public AppSettingsViewModel() : this(new AppSettings()) =>
      RequireTrue(IsInDesignMode);

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
        _paletteHelper.ChangeBaseTheme(value);
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

    private static IHueViewModel? FindHueViewModel(ColorScheme colorScheme, Arr<ISwatchViewModel> swatchViewModels, IAppSettings appSettings)
    {
      string? colorName;
      int colorHue;

      switch (colorScheme)
      {
        case ColorScheme.Primary:
          colorName = appSettings.PrimaryColorName;
          colorHue = appSettings.PrimaryColorHue;
          break;

        case ColorScheme.PrimaryForeground:
          colorName = appSettings.PrimaryForegroundColorName;
          colorHue = appSettings.PrimaryForegroundColorHue;
          break;

        case ColorScheme.Secondary:
          colorName = appSettings.SecondaryColorName;
          colorHue = appSettings.SecondaryColorHue;
          break;

        case ColorScheme.SecondaryForeground:
          colorName = appSettings.SecondaryForegroundColorName;
          colorHue = appSettings.SecondaryForegroundColorHue;
          break;

        default: throw new ArgumentOutOfRangeException(nameof(colorScheme));
      }

      if (colorName.IsntAString()) return default;

      var swatchViewModel = swatchViewModels
        .Find(svm => svm.Swatch.Name.EqualsCI(colorName))
        .AssertSome($"Unknown swatch name: {colorName}");

      return swatchViewModel.HueViewModels[colorHue];
    }

    private void HandleChangeHue(IHueViewModel hueViewModel)
    {
      var currentlySelected = FindHueViewModel(ActiveScheme, SwatchViewModels, _appSettings);
      var isUnset = false;

      if (currentlySelected != default)
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
          _paletteHelper.ChangePrimaryColor(hueViewModel.Hue);
          _appSettings.PrimaryColorName = hueViewModel.Swatch.Name;
          _appSettings.PrimaryColorHue = hueViewModel.HueIndex;
          break;

        case ColorScheme.PrimaryForeground:
          if (isUnset)
          {
            _paletteHelper.ChangePrimaryForegroundColor(default);
            _appSettings.PrimaryForegroundColorName = default;
          }
          else
          {
            _paletteHelper.ChangePrimaryForegroundColor(hueViewModel.Hue);
            _appSettings.PrimaryForegroundColorName = hueViewModel.Swatch.Name;
            _appSettings.PrimaryForegroundColorHue = hueViewModel.HueIndex;
          }
          break;

        case ColorScheme.Secondary:
          _paletteHelper.ChangeSecondaryColor(hueViewModel.Hue);
          _appSettings.SecondaryColorName = hueViewModel.Swatch.Name;
          _appSettings.SecondaryColorHue = hueViewModel.HueIndex;
          SetSecondaryHueHexes();
          break;

        case ColorScheme.SecondaryForeground:
          if (isUnset)
          {
            _paletteHelper.ChangeSecondaryForegroundColor(default);
            _appSettings.SecondaryForegroundColorName = default;
          }
          else
          {
            _paletteHelper.ChangeSecondaryForegroundColor(hueViewModel.Hue);
            _appSettings.SecondaryForegroundColorName = hueViewModel.Swatch.Name;
            _appSettings.SecondaryForegroundColorHue = hueViewModel.HueIndex;
          }
          break;
      }

      if (!isUnset) hueViewModel.IsSelected = true;
    }

    private void ChangeToScheme(ColorScheme colorScheme)
    {
      var currentlySelected = FindHueViewModel(ActiveScheme, SwatchViewModels, _appSettings);
      if (currentlySelected != default) currentlySelected.IsSelected = false;

      ActiveScheme = colorScheme;

      currentlySelected = FindHueViewModel(ActiveScheme, SwatchViewModels, _appSettings);
      if (currentlySelected != default) currentlySelected.IsSelected = true;
    }

    private void SetSecondaryHueHexes()
    {
      static string int2Hex(int i) => i.ToString("X").ToLower();

      static string colorToHex(Color c) => "#" +
        int2Hex(c.R) +
        int2Hex(c.G) +
        int2Hex(c.B);

      var hueViewModel = FindHueViewModel(ColorScheme.Secondary, SwatchViewModels, _appSettings).AssertNotNull();
      var hue = hueViewModel.Hue;

      SecondaryHueLightHex = colorToHex(hue.Lighten());
      SecondaryHueMidHex = colorToHex(hue);
      SecondaryHueDarkHex = colorToHex(hue.Darken());

      hueViewModel = FindHueViewModel(ColorScheme.SecondaryForeground, SwatchViewModels, _appSettings);
      if (hueViewModel == default)
      {
        hue = hue.ContrastingForegroundColor();
      }
      else
      {
        hue = hueViewModel.Hue;
      }

      SecondaryHueMidForegroundHex = colorToHex(hue);
    }

    private readonly PaletteHelper _paletteHelper = new PaletteHelper();
    private readonly IAppSettings _appSettings;
  }
}
