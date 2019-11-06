using Ninject;
using RVisUI.Extensions;
using RVisUI.Ioc;
using RVisUI.Properties;
using System;
using System.Configuration;
using System.Linq;
using System.Windows;

namespace RVisUI
{
  public partial class App
  {
    private void DoStartup(StartupEventArgs e)
    {
      var logLevelSuffix = ".Log.Level";
      var logNames = ConfigurationManager.AppSettings.AllKeys
        .Where(k => k.EndsWith(logLevelSuffix, StringComparison.InvariantCulture))
        .Select(k => k.Substring(0, k.Length - logLevelSuffix.Length))
        .ToArray();
      RVis.Base.Logging.Configure(logNames);

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
      AppState.Initialize(e.Args);
    }

    private void HandleExit(object sender, ExitEventArgs e)
    {
      (AppState as IDisposable).Dispose();
      (AppService as IDisposable).Dispose();
    }
  }
}
