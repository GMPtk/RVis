using RVis.Model;

namespace Plot.Design
{
  public class LogEntryViewModel : ILogEntryViewModel
  {
    public SimDataLogEntry LogEntry { get; set; }

    public string EnteredOn { get; set; }

    public string RequesterTypeName { get; set; }

    public string ParameterAssignments { get; set; }
  }
}
