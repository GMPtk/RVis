using NLog;
using RDotNet;
using RVis.Base;
using System;
using System.Collections.Generic;

namespace RVis.ROps
{
  public static partial class ROpsApi
  {
    static ROpsApi()
    {
      REngine.SetEnvironmentVariables();
    }

    public static Dictionary<string, string> GetRversion()
    {
      var info = new Dictionary<string, string>();

      var instance = REngine.GetInstance();
      var list = instance.Evaluate("sessionInfo()").AsList();
      var rVersion = list["R.version"].AsList();

      foreach (var name in rVersion.Names)
      {
        var value = rVersion[name].AsCharacter()[0];
        info.Add(name, value);
      }

      return info;
    }

    public static Dictionary<string, string> GetInstalledPackages()
    {
      var installedPackages = new Dictionary<string, string>();

      var instance = REngine.GetInstance();
      var matrix = instance.Evaluate("installed.packages()").AsCharacterMatrix();
      var columnNames = matrix.ColumnNames;
      var packageColumn = Array.IndexOf(columnNames, "Package");
      var versionColumn = Array.IndexOf(columnNames, "Version");

      var nRows = matrix.RowCount;
      for (var row = 0; row < nRows; ++row)
      {
        var package = matrix[row, packageColumn];
        var version = matrix[row, versionColumn];
        installedPackages.Add(package, version);
      }

      return installedPackages;
    }

    public static void ClearGlobalEnvironment()
    {
      _pathToLastSourcedFile = default;
      _lastSourcedFileLastWriteTime = default;
      _execFormalType = default;

      var instance = REngine.GetInstance();
      instance.ClearGlobalEnvironment();
    }

    public static void GarbageCollect()
    {
      var instance = REngine.GetInstance();
      instance.Evaluate("gc()");
    }

    private static readonly ILogger _log = Logging.Create(nameof(ROpsApi) + ".All");
  }
}
