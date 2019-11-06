using NLog;
using NLog.Config;
using NLog.Targets;
using RVis.Base.Extensions;
using System;
using System.Configuration;
using System.IO;
using static RVis.Base.Check;
using static System.Globalization.CultureInfo;

namespace RVis.Base
{
  public static class Logging
  {
    public static void Configure(params string[] names)
    {
      Configure(names, null);
    }

    public static void Configure(string name, string logLevel)
    {
      Configure(new[] { name }, LogLevel.FromString(logLevel));
    }

    public static void Configure(string[] names, LogLevel logLevel = default)
    {
      RequireNotNull(names);

      var directory = Path.Combine(DirectoryOps.ApplicationDataDirectory.FullName, "Log");
      if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

      var loggingConfiguration = new LoggingConfiguration();
      var layout = @"${date:format=HH\:mm\:ss} ${message} ${exception:format=toString}";
      var fileNameBase = DateTime.UtcNow.ToString("o", InvariantCulture).ToValidFileName();

      foreach (var name in names)
      {
        var fileTarget = new FileTarget();
        loggingConfiguration.AddTarget("file." + name, fileTarget);

        var fileName = $"{name}.{fileNameBase}.log";
        fileTarget.FileName = Path.Combine(directory, fileName);

        fileTarget.Layout = layout;

        var minLevel = logLevel;
        if (minLevel == default)
        {
          var minLevelAsString = ConfigurationManager.AppSettings[$"{name}.Log.Level"];
          if (minLevelAsString.IsAString()) minLevel = LogLevel.FromString(minLevelAsString);
        }
        minLevel = minLevel ?? LogLevel.Off;

        var rule = new LoggingRule($"{name}.*", minLevel, fileTarget);
        loggingConfiguration.LoggingRules.Add(rule);
      }

      LogManager.Configuration = loggingConfiguration;
    }

    public static ILogger Create(string name) => LogManager.GetLogger(name);
  }
}
