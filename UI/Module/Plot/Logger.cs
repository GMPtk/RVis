using NLog;

namespace Plot
{
  internal static class Logger
  {
    internal static ILogger Log => _log ??= RVis.Base.Logging.Create($"{nameof(Plot)}.All");
    private static ILogger? _log;
  }
}
