using LanguageExt;
using Nett;
using RVis.Base.Extensions;
using System;
using System.Diagnostics;
using System.IO;
using static LanguageExt.Prelude;
using static RVis.Model.Constant;
using static RVis.Model.Sim;
using static RVis.Base.Check;

namespace RVis.Model.Extensions
{
  public static partial class SimExt
  {
    internal static bool IsRSimulation(this Simulation simulation) =>
      simulation.SimConfig.SimCode.File?.EndsWith(".R", StringComparison.InvariantCultureIgnoreCase) == true;

    internal static bool IsMCSimSimulation(this Simulation simulation) =>
      simulation.SimConfig.SimCode.File?.EndsWith(".exe", StringComparison.InvariantCultureIgnoreCase) == true;

    internal static Option<SimConfig> LoadConfig(this Simulation simulation, string inputHash)
    {
      if (simulation.SimConfig.SimInput.Hash == inputHash) return Some(simulation.SimConfig);

      var pathToData = GetPathToData(simulation, inputHash);
      var pathToDataDirectory = Path.GetDirectoryName(pathToData);
      RequireNotNullEmptyWhiteSpace(pathToDataDirectory);
      if (!Directory.Exists(pathToDataDirectory)) return None;

      var pathToEditsFile = Path.Combine(pathToDataDirectory, EditsFileName);
      if (!File.Exists(pathToEditsFile)) return None;

      var fromToml = Toml.ReadFile<TSimInput>(pathToEditsFile);

      var edits = FromToml(fromToml);

      var input = simulation.SimConfig.SimInput.With(edits.SimParameters);
      var config = simulation.SimConfig.With(input);
      return Some(config);
    }

    public static bool IsExecType(this Simulation simulation) =>
      simulation.SimConfig.SimCode.Exec.IsAString();

    public static bool IsTmplType(this Simulation simulation) =>
      !simulation.IsExecType();

    public static void WriteToFile(this SimConfig config, string pathToSimulation)
    {
      var pathToPrivate = Path.Combine(pathToSimulation, PRIVATE_SUBDIRECTORY);
      if (!Directory.Exists(pathToPrivate)) Directory.CreateDirectory(pathToPrivate);

      var pathToConfig = Path.Combine(pathToPrivate, CONFIG_FILE_NAME);
      var toToml = ToToml(config);
      Toml.WriteFile(toToml, pathToConfig);
    }

    internal static readonly string SessionLogFileName = $"{Environment.ProcessId}.{LogFileExtension}";
    internal const string FmtLogFilesDirectory = "yyyy-MM-dd";
    internal const string LogFileExtension = "log";
    internal const string EditsFileName = "parameters.toml";
  }
}
