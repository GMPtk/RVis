using NLog;
using RVisUI.Properties;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace RVisUI
{
  public partial class App
  {
    private void ConfigureDiagnostics()
    {
      if (Settings.Default.RecordRuntimeExceptions)
      {
        DispatcherUnhandledException += (_, e) =>
        {
          Log.Error(e.Exception, nameof(DispatcherUnhandledException));

          MessageBox.Show(e.Exception.Message, "Application Fault", MessageBoxButton.OK, MessageBoxImage.Error);

          e.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
          Exception exception;
          if (e.ExceptionObject is Exception ex)
          {
            exception = ex;
          }
          else if (null != e.ExceptionObject)
          {
            exception = new Exception(e.ExceptionObject.ToString());
          }
          else
          {
            exception = new Exception("Unknown error");
          }

          Log.Error(
            exception,
            $"{nameof(AppDomain)}{nameof(AppDomain.CurrentDomain.UnhandledException)}"
            );
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
          Log.Error(
            e.Exception,
            $"{nameof(TaskScheduler)}{nameof(TaskScheduler.UnobservedTaskException)}"
            );

          e.SetObserved();
        };
      }
    }

    internal ILogger Log => _logger ??= RVis.Base.Logging.Create($"{nameof(RVisUI)}{nameof(App)}.All");
    private ILogger? _logger;
  }
}
