using NLog;

namespace Estimation
{
  internal static class Logger
  {
    internal static ILogger Log => _log ??= RVis.Base.Logging.Create($"{nameof(Estimation)}.All");
    private static ILogger? _log;
  }
}
