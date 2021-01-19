using NLog;

namespace RVisUI.Model
{
  internal static class Logger
  {
    internal static ILogger Log => _log ??= RVis.Base.Logging.Create($"{nameof(RVisUI)}{nameof(Model)}.All");
    private static ILogger? _log;
  }
}
