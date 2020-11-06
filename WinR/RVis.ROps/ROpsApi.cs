using NLog;
using RDotNet;
using RVis.Base;
using RVis.Base.Extensions;
using System;
using System.Collections.Generic;
using static System.Configuration.ConfigurationManager;
using static System.Environment;

namespace RVis.ROps
{
  public static partial class ROpsApi
  {
    static ROpsApi()
    {
      var rHome = AppSettings["ROpsApi.RHome"];

      if (rHome.IsAString())
      {
        var binDir = Is64BitProcess
          ? @"\bin\x64"
          : @"\bin\i386"
          ;

        var rPath = rHome + binDir;

        REngine.SetEnvironmentVariables(rPath, rHome);
      }
      else
      {
        REngine.SetEnvironmentVariables();
      }
    }

    public static (string Name, string Value)[] GetRversion()
    {
      var info = new List<(string Name, string Value)>();

      var instance = REngine.GetInstance();
      var list = instance.Evaluate("sessionInfo()").AsList();
      var rVersion = list["R.version"].AsList();

      foreach (var name in rVersion.Names)
      {
        var value = rVersion[name].AsCharacter()[0];
        info.Add((name, value));
      }

      return info.ToArray();
    }

    public static (string Package, string Version)[] GetInstalledPackages()
    {
      var installedPackages = new List<(string Package, string Version)>();

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
        installedPackages.Add((package, version));
      }

      return installedPackages.ToArray();
    }

    public static void Clear()
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
