using Microsoft.Extensions.Configuration;
using NLog;
using RDotNet;
using RVis.Base;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RVis.ROps
{
  public static partial class ROpsApi
  {
    static ROpsApi()
    {
      var basePath = AppContext.BaseDirectory;

#if DEBUG
      if(!System.IO.File.Exists(System.IO.Path.Join(basePath, "appsettings.json")))
      {
        var index = basePath.IndexOf(@"\Test\");
        var pathToRVis = basePath[..index];
        basePath = System.IO.Path.Join(pathToRVis, "R", "RVis.Server");
      }
#endif

      var builder = new ConfigurationBuilder()
        .SetBasePath(basePath)
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

      var configuration = builder.Build();

      var rOpsApi = configuration.GetSection(nameof(ROpsApi));
      var settings = rOpsApi
        .GetChildren()
        .ToDictionary(cs => cs.Key, cs => cs.Value);
      var rPath = settings.ContainsKey("RPath") ? settings["RPath"] : null;
      var rHome = settings.ContainsKey("RHome") ? settings["RHome"] : null;

      REngine.SetEnvironmentVariables(rPath, rHome);

      if (settings.ContainsKey("RLib"))
      {
        var rLib = settings["RLib"].Replace("\\", "/");
        var command = $"pathtolib <- \"{rLib}\"; .libPaths(c(pathtolib, .libPaths()))";
        REngine.GetInstance().Evaluate(command);
      }
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
