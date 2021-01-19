using NLog;

namespace RVisUI.Mvvm
{
  internal static class Logger
  {
    internal static ILogger Log => _log ??= RVis.Base.Logging.Create($"{nameof(RVisUI)}{nameof(Mvvm)}.All");
    private static ILogger? _log;
  }
}
