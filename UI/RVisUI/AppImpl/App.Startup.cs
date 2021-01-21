using Microsoft.Extensions.Configuration;
using Ninject;
using NLog;
using RVis.Base.Extensions;
using RVisUI.Extensions;
using RVisUI.Ioc;
using RVisUI.Model;
using RVisUI.Properties;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using static RVisUI.Wpf.Behaviour;
using static System.IO.Path;

namespace RVisUI
{
  public partial class App
  {
    private void DoStartup(StartupEventArgs e)
    {
      var builder = new ConfigurationBuilder()
        .SetBasePath(GetBasePath())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

      var configuration = builder.Build();

      var logLevel = configuration.GetSection(nameof(LogLevel));
      var logLevels = logLevel
        .GetChildren()
        .ToDictionary(cs => cs.Key, cs => LogLevel.FromString(cs.Value));
      RVis.Base.Logging.Configure(logLevels);

      var rvisUI = configuration.GetSection(nameof(RVisUI));
      RVisUI.MainWindow.ShowFrameRate = rvisUI.GetValue<bool>(
        nameof(RVisUI.MainWindow.ShowFrameRate)
        );
      DocRoot = rvisUI.GetValue<string>(
        nameof(DocRoot)
        );

      var doSaveSettings = false;

      if (Settings.Default.UpgradeRequired)
      {
        Settings.Default.Upgrade();
        Settings.Default.UpgradeRequired = false;
        doSaveSettings = true; // TODO: do it here?
      }

      Settings.Default.ApplyTheme();

      if (0 == Settings.Default.RThrottlingUseCores)
      {
        Settings.Default.RThrottlingUseCores = Environment.ProcessorCount - 1;
        doSaveSettings = true;
      }

      if (doSaveSettings)
      {
        Settings.Default.Save();
      }

      NinjectKernel.Load(new AppModule());

      ViewModelLocator.SubscribeToServiceTypes(
        GetType().Assembly,
        $"{nameof(RVisUI)}.{nameof(Ioc)}.{nameof(Mvvm)}"
        );

      ConfigureDiagnostics();

      Exit += HandleExit;

      AppService.Initialize();

      var appState = new AppState(AppSettings, AppService);
      NinjectKernel.Bind<IAppState>().ToConstant(appState);

      try
      {
        appState.Initialize(e.Args);
      }
      catch (Exception ex)
      {
        AppService.Notify(
          nameof(AppState),
          "Start-up",
          ex
          );
        Log.Error(ex);
        Shutdown(1);
      }
    }

    private void HandleExit(object sender, ExitEventArgs e)
    {
      ((IDisposable)AppState).Dispose();
      ((IDisposable)AppService).Dispose();
    }

    private static string GetBasePath()
    {
      using var processModule = Process.GetCurrentProcess().MainModule;
      return GetDirectoryName(processModule?.FileName)
        .AssertNotNull($"Failed to find process main module image directory");
    }
  }
}
