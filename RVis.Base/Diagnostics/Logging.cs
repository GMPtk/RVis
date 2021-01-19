using NLog;
using NLog.Config;
using NLog.Targets;
using RVis.Base.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using static RVis.Base.Check;
using static System.Globalization.CultureInfo;

namespace RVis.Base
{
  public static class Logging
  {
    public static void Configure(IDictionary<string, LogLevel> logLevels)
    {
      RequireNotNull(logLevels);

      var directory = Path.Combine(DirectoryOps.ApplicationDataDirectory.FullName, "Log");
      if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

      var loggingConfiguration = new LoggingConfiguration();
      var layout = @"${date:format=HH\:mm\:ss} ${message} ${exception:format=toString}";
      var fileNameBase = DateTime.UtcNow.ToString("o", InvariantCulture).ToValidFileName();

      foreach (var name in logLevels.Keys)
      {
        var fileTarget = new FileTarget();
        loggingConfiguration.AddTarget("file." + name, fileTarget);

        var fileName = $"{name}.{fileNameBase}.log";
        fileTarget.FileName = Path.Combine(directory, fileName);

        fileTarget.Layout = layout;

        var rule = new LoggingRule($"{name}.*", logLevels[name], fileTarget);
        loggingConfiguration.LoggingRules.Add(rule);
      }

      LogManager.Configuration = loggingConfiguration;
    }

    public static ILogger Create(string name) => LogManager.GetLogger(name);
  }
}
