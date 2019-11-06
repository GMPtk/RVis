using LanguageExt;
using RVis.Base.Extensions;
using System;
using System.IO;
using System.Linq;
using static RVis.Base.Check;
using static RVis.Model.Constant;
using static System.Globalization.CultureInfo;

namespace RVis.Model.Extensions
{
  public static partial class SimExt
  {
    public static string GetPathToSessionLogDirectory(this Simulation simulation) =>
      simulation.GetPrivateDirectory(
        new[] { "data", "log", DateTime.Now.ToString(FmtLogFilesDirectory, InvariantCulture) }
      );

    public static string GetPathToLogDirectory(this Simulation simulation) =>
      simulation.GetPrivateDirectory(new[] { "data", "log" });

    public static string GetPathToSessionLog(this Simulation simulation) =>
      simulation.GetPrivatePath(
        new[] { "data", "log", DateTime.Now.ToString(FmtLogFilesDirectory, InvariantCulture) },
        SessionLogFileName
      );

    public static string GetPrivateDirectory(this Simulation simulation, params string[] paths)
    {
      paths = paths.Select(c => c.ToValidFileName().ToLowerInvariant()).ToArray();
      RequireFalse(paths.Any(p => p.IsntAString()));
      return Path.Combine(GetPathToPrivate(simulation), Path.Combine(paths));
    }

    public static string GetPrivatePath(this Simulation simulation, string[] paths, string fileName)
    {
      var directory = GetPrivateDirectory(simulation, paths);
      return Path.Combine(directory, fileName);
    }

    private static string GetPathToPrivate(this Simulation simulation) =>
      Path.Combine(simulation.PathToSimulation, PRIVATE_SUBDIRECTORY);

    public static string GetPathToDataDirectory(this Simulation simulation, string inputHash) =>
      Path.Combine(simulation.GetPathToPrivate(), DATA_SUBDIRECTORY, inputHash);

    private static string GetPathToData(this Simulation simulation, string inputHash) =>
      Path.Combine(simulation.GetPathToDataDirectory(inputHash), DATA_BIN_FILE_NAME);
  }
}
