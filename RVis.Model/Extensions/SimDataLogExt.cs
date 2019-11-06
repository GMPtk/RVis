using RVis.Base.Extensions;

namespace RVis.Model.Extensions
{
  public static class SimDataLogExt
  {
    public static bool IsForOriginalConfig(this SimDataLogEntry logEntry) =>
      logEntry.ParameterAssignments.IsntAString();
  }
}
