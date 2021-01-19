using NLog;

namespace Sensitivity
{
  internal static class Logger
  {
    internal static ILogger Log => _log ??= RVis.Base.Logging.Create($"{nameof(Sensitivity)}.All");
    private static ILogger? _log;
  }
}
