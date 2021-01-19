using NLog;

namespace RVis.Model
{
  internal static class Logger
  {
    internal static ILogger Log => _log ??= Base.Logging.Create($"{nameof(RVis)}{nameof(Model)}.All");
    private static ILogger _log = null!;
  }
}
