using NLog;

namespace Sampling
{
  internal static class Logger
  {
    internal static ILogger Log => _log ??= RVis.Base.Logging.Create($"{nameof(Sampling)}.All");
    private static ILogger? _log;
  }
}
