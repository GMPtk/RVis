using LanguageExt;
using MahApps.Metro.Controls;
using ReactiveUI;
using RVis.Base;
using RVis.Model;
using RVis.Model.Extensions;
using RVisUI.Controls.Dialogs;
using RVisUI.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using static RVis.Base.Check;
using static RVis.Base.ProcessHelper;
using static System.Double;
using static System.Math;

namespace RVisUI
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : MetroWindow
  {
    public MainWindow()
    {
      InitializeComponent();

      RequireNotNull(SynchronizationContext.Current);

      ToggleFullScreenCommand = ReactiveCommand.Create(() => FullScreen = !FullScreen);

      _tbResetRServicesFG = _tbResetRServices.Foreground;

      if (ShowFrameRate)
      {
        CompositionTarget.Rendering += HandleCompositionTargetRendering;
        _stopwatch.Start();
      }

      Loaded += HandleLoaded;
    }

    internal static bool ShowFrameRate { get; set; } = false;

    internal static double MaximumMemoryPressure { get; set; } = 1d;

    private void HandleLoaded(object sender, RoutedEventArgs e)
    {
      RequireNotNull(SynchronizationContext.Current);

      Loaded -= HandleLoaded;

      App.Current.AppService.RVisServerPool.ServerLicenses
        .ObserveOn(SynchronizationContext.Current)
        .Subscribe(ObserveServerLicense);

      App.Current.AppState?.SimData?.OutputRequests
        .ObserveOn(SynchronizationContext.Current)
        .Subscribe(ObserveOutputRequest);

      App.Current.AppSettings
        .ObservableForProperty(@as => @as.Zoom)
        .Subscribe(_ => ScaleClient());
    }

    public double FrameRate
    {
      get { return (double)GetValue(FrameRateProperty); }
      set { SetValue(FrameRateProperty, value); }
    }

    public static readonly DependencyProperty FrameRateProperty =
      DependencyProperty.Register(
        nameof(FrameRate),
        typeof(double),
        typeof(MainWindow),
        new UIPropertyMetadata(default(double))
        );

    public ICommand ToggleFullScreenCommand { get; private set; }

    public static readonly DependencyProperty FullScreenProperty =
      DependencyProperty.Register(
        "FullScreen",
        typeof(bool),
        typeof(MainWindow),
        new PropertyMetadata(default(bool), FullScreenPropertyChangedCallback));

    private static void FullScreenPropertyChangedCallback(
      DependencyObject dependencyObject,
      DependencyPropertyChangedEventArgs e
      )
    {
      var metroWindow = (MetroWindow)dependencyObject;
      if (e.OldValue != e.NewValue)
      {
        var fullScreen = (bool)e.NewValue;
        if (fullScreen)
        {
          metroWindow.UseNoneWindowStyle = true;
          metroWindow.IgnoreTaskbarOnMaximize = true;
          metroWindow.WindowState = WindowState.Maximized;
        }
        else
        {
          metroWindow.UseNoneWindowStyle = false;
          metroWindow.ShowTitleBar = true; // <-- this must be set to true
          metroWindow.IgnoreTaskbarOnMaximize = false;
          metroWindow.WindowState = WindowState.Normal;
        }
      }
    }

    public bool FullScreen
    {
      get { return (bool)GetValue(FullScreenProperty); }
      set { SetValue(FullScreenProperty, value); }
    }

    public static readonly DependencyProperty ClientScaleProperty =
      DependencyProperty.Register(
        nameof(ClientScale),
        typeof(double),
        typeof(MainWindow),
        new UIPropertyMetadata(1.0)
        );

    public double ClientScale
    {
      get => (double)GetValue(ClientScaleProperty);
      set => SetValue(ClientScaleProperty, value);
    }

    private void HandleToggleFullScreen(object sender, RoutedEventArgs e) =>
      FullScreen = !FullScreen;

    private void HandleOpenLogDirectory(object sender, RoutedEventArgs e)
    {
      var directory = Path.Combine(DirectoryOps.ApplicationDataDirectory.FullName, "Log");
      if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
      OpenUrl(directory);
    }

    private void HandleConfigureModules(object sender, RoutedEventArgs e)
    {
      var configureModulesDialog = new ConfigureModulesDialog { Owner = this };
      configureModulesDialog.ShowDialog();
    }

    private void HandleResetRServices(object sender, RoutedEventArgs e)
    {
      var yes = App.Current.AppService.AskUserYesNoQuestion(
        "Proceed?",
        "You are about to shutdown any active R process(es) and restart the data services. The application may halt for a short time while this is taking place.",
        "Reset R Services"
        );

      if (yes)
      {
        App.Current.AppState.ResetSimDataService();
        App.Current.AppService.ResetRVisServerPool();
      }
    }

    private void ObserveServerLicense((ServerLicense ServerLicense, bool HasExpired) item)
    {
      var (serverLicense, hasExpired) = item;

      if (hasExpired)
      {
        _currentLicenses.Remove(serverLicense.ID);
      }
      else
      {
        _currentLicenses.Add(serverLicense.ID, serverLicense);
      }

      _tbResetRServices.Foreground = _currentLicenses.Any() ? Brushes.Red : _tbResetRServicesFG;
    }

    private void ObserveOutputRequest(SimDataItem<OutputRequest> simDataItem)
    {
      if (simDataItem.IsResetEvent())
      {
        _currentLicenses.Clear();
        _tbResetRServices.Foreground = _tbResetRServicesFG;
      }
      else
      {
        CheckMemoryPressure();
      }
    }

    private void HandleCompositionTargetRendering(object? sender, EventArgs e)
    {
      ++_frameCount;

      if (_stopwatch.ElapsedMilliseconds > 1000 && _frameCount > 1)
      {
        FrameRate = 1000.0 * _frameCount / _stopwatch.ElapsedMilliseconds;
        _frameCount = 0;
        _stopwatch.Reset();
        _stopwatch.Start();
      }
    }

    private void CheckMemoryPressure()
    {
      if (++_memoryEventCount % MEMORY_PRESSURE_CHECK_INTERVAL != 0) return;

      _process ??= Process.GetCurrentProcess();

      _process.Refresh();

      var privateMemorySize64 = _process.PrivateMemorySize64;

      var memoryPressure = (0d + privateMemorySize64) / MAXIMUM_PROCESS_MEMORY;

      if (memoryPressure > MaximumMemoryPressure)
      {
        App.Current.AppState.SimData.Clear(includePendingRequests: false);
        GC.Collect();
      }
    }

    private void HandleSizeChanged(object sender, EventArgs e)
    {
      ScaleClient();
    }

    private void ScaleClient()
    {
      var zoom = Settings.Default.Zoom;
      var yScale = zoom * ActualHeight / 768d;
      var xScale = zoom * ActualWidth / 1366d;
      var value = Min(xScale, yScale);
      ClientScale = IsNaN(value) ? 1d : value;
    }

    private const int MEMORY_PRESSURE_CHECK_INTERVAL = 200;
    private readonly long MAXIMUM_PROCESS_MEMORY = Environment.Is64BitProcess
      ? 16L * 1024 * 1024 * 1024 * 1024
      : Environment.Is64BitOperatingSystem
        ? 4L * 1024 * 1024 * 1024
        : 2L * 1024 * 1024 * 1024;

    private readonly IDictionary<int, ServerLicense> _currentLicenses = new SortedDictionary<int, ServerLicense>();
    private readonly Brush _tbResetRServicesFG;
    private readonly Stopwatch _stopwatch = new Stopwatch();
    private int _frameCount;
    private int _memoryEventCount;
    private Process? _process;
  }
}
