using LanguageExt;
using System;
using static LanguageExt.Prelude;

namespace RVis.Model
{
  public class SimDataLogEntry
  {
    internal static Option<SimDataLogEntry> Parse(string line)
    {
      var parts = line.Split('$');
      if (parts.Length != 6 ||
          !int.TryParse(parts[0], out int id) ||
          !DateTime.TryParse(parts[1], out DateTime enteredOn))
      {
        Logger.Log.Warn($"Failed to parse log entry: {line}");
        return None;
      }
      return new SimDataLogEntry(
        id,
        enteredOn,
        parts[2],
        parts[3],
        parts[4],
        parts[5]
        );
    }

    internal SimDataLogEntry(int id, DateTime enteredOn, string serieInputHash, string parameterAssignments, string requesterTypeName, string description)
    {
      ID = id;
      EnteredOn = enteredOn;
      SerieInputHash = serieInputHash;
      ParameterAssignments = parameterAssignments;
      RequesterTypeName = requesterTypeName;
      Description = description;
    }

    public int ID { get; }
    public DateTime EnteredOn { get; }
    public string SerieInputHash { get; }
    public string ParameterAssignments { get; }
    public string RequesterTypeName { get; }
    public string Description { get; }

    public override string ToString() =>
      $"{ID:00000000}${EnteredOn:o}${SerieInputHash}${ParameterAssignments}${RequesterTypeName}${Description}";
  }
}
