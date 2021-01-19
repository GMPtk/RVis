using LanguageExt;
using Nett;
using RVis.Base.Extensions;
using RVis.Data;
using System;
using System.Collections.Generic;
using System.IO;
using static LanguageExt.Prelude;
using static RVis.Base.Check;
using static RVis.Model.Constant;

namespace RVis.Model.Extensions
{
  public static partial class SimExt
  {
    public static bool HasData(this Simulation simulation, SimInput input)
    {
      var pathToData = GetPathToData(simulation, input.Hash);
      return File.Exists(pathToData);
    }

    public static NumDataTable LoadData(this Simulation simulation, SimInput input)
    {
      var pathToData = GetPathToData(simulation, input.Hash);
      RequireFile(pathToData);
      return NumDataTable.LoadFromBinaryFile(pathToData);
    }

    public static void SaveData(this Simulation simulation, NumDataTable data, SimInput input)
    {
      var pathToData = GetPathToData(simulation, input.Hash);
      if (File.Exists(pathToData))
      {
        throw new InvalidOperationException($"Expecting empty path to save data: {pathToData}");
      }

      var pathToDataDirectory = Path.GetDirectoryName(pathToData);
      RequireNotNullEmptyWhiteSpace(pathToDataDirectory);
      if (!Directory.Exists(pathToDataDirectory)) Directory.CreateDirectory(pathToDataDirectory);

      NumDataTable.SaveToBinaryFile(data, pathToData);

      var edits = input.SimParameters.Filter(p =>
      {
        var search = simulation.SimConfig.SimInput.SimParameters.Find(q => q.Name == p.Name);
        var original = search.AssertSome($"Unknown parameter {p.Name}");
        return p.Value != original.Value;
      });

      if (!edits.IsEmpty)
      {
        var edited = simulation.SimConfig.SimInput.With(edits);
        Toml.WriteFile(edited, Path.Combine(pathToDataDirectory, EditsFileName));
      }
    }

    public static void SavePrivateData<T>(this Simulation simulation, T data, string group, string category, string instance)
    {
      var path = GetPrivatePath(
        simulation,
        new[] { group, category, instance },
        PRIVATE_DATA_FILE_NAME
        );

      if (EqualityComparer<T>.Default.Equals(data, default))
      {
        if (File.Exists(path)) File.Delete(path);
        return;
      }

      Toml.WriteFile(data, path);
    }

    public static Option<T> LoadPrivateData<T>(this Simulation simulation, string group, string category, string instance)
    {
      var path = GetPrivatePath(
        simulation,
        new[] { group, category, instance },
        PRIVATE_DATA_FILE_NAME
        );

      if (!File.Exists(path)) return None;

      return Toml.ReadFile<T>(path);
    }
  }
}
