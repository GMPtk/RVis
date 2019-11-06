using RVis.Model;
using RVis.Model.Extensions;
using static System.Globalization.CultureInfo;
using static System.String;

namespace Plot
{
  public class LogEntryViewModel : ILogEntryViewModel
  {
    public LogEntryViewModel(SimDataLogEntry logEntry, SimInput input)
    {
      LogEntry = logEntry;
      EnteredOn = logEntry.EnteredOn.ToString("yyyy-MM-dd HH:mm:ss", InvariantCulture);
      RequesterTypeName = logEntry.RequesterTypeName;

      var parameters = input.GetParameters(logEntry.ParameterAssignments);
      ParameterAssignments = Join(", ", parameters.Map(p => p.ToAssignment("G4")));
    }

    public SimDataLogEntry LogEntry { get; }

    public string EnteredOn { get; }

    public string RequesterTypeName { get; }

    public string ParameterAssignments { get; }
  }
}
