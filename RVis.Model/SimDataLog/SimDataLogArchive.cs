using RVis.Model.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static System.Globalization.CultureInfo;

namespace RVis.Model
{
  public class SimDataLogArchive
  {
    public SimDataLogArchive(Simulation simulation)
    {
      _simulation = simulation;
    }

    public Task<IReadOnlyCollection<SimDataLogEntry>> GetTodayAsync()
    {
      if (null != _today) return Task.FromResult(_today);

      return Task.Run(() =>
      {
        _today = LoadToday(_simulation);
        return _today;
      });
    }

    public Task<IReadOnlyCollection<SimDataLogEntry>> GetThisMonthAsync()
    {
      if (null != _thisMonth) return Task.FromResult(_thisMonth);

      return Task.Run(() =>
      {
        _thisMonth = LoadThisMonth(_simulation);
        return _thisMonth;
      });
    }

    public Task<IReadOnlyCollection<SimDataLogEntry>> GetThisYearAsync()
    {
      if (null != _thisYear) return Task.FromResult(_thisYear);

      return Task.Run(() =>
      {
        _thisYear = LoadThisYear(_simulation);
        return _thisYear;
      });
    }

    public Task<IReadOnlyCollection<SimDataLogEntry>> GetPreviousYearsAsync()
    {
      if (null != _previousYears) return Task.FromResult(_previousYears);

      return Task.Run(() =>
      {
        _previousYears = LoadPreviousYears(_simulation);
        return _previousYears;
      });
    }

    private static SimDataLogEntry[] LoadToday(Simulation simulation)
    {
      var pathToSessionLogDirectory = simulation.GetPathToSessionLogDirectory();
      var directory = new DirectoryInfo(pathToSessionLogDirectory);
      if (!directory.Exists)
      {
        return Array.Empty<SimDataLogEntry>();
      }
      var logFiles = directory.GetFiles($"*.{SimExt.LogFileExtension}");
      var sessionLogEntries = new List<SimDataLogEntry>();
      foreach (var logFile in logFiles)
      {
        if (logFile.Name == SimExt.SessionLogFileName) continue;

        try
        {
          var lines = File.ReadAllLines(logFile.FullName);
          var logEntries = lines.Select(l => SimDataLogEntry.Parse(l)).Somes();
          sessionLogEntries.AddRange(logEntries);
        }
        catch (Exception ex)
        {
          Logger.Log.Error(ex, $"Failed to read log file {logFile.FullName}");
        }
      }
      return sessionLogEntries.OrderBy(le => le.EnteredOn).ToArray();
    }

    private static SimDataLogEntry[] LoadThisMonth(Simulation simulation)
    {
      var pathToLogDirectory = simulation.GetPathToLogDirectory();
      if (!Directory.Exists(pathToLogDirectory))
      {
        return Array.Empty<SimDataLogEntry>();
      }
      var now = DateTime.Now;
      var directoryPrefix = $"{now.ToString("yyyy-MM", InvariantCulture)}-";
      var today = now.Day;

      var logEntries = new List<SimDataLogEntry>();
      for (var i = 1; i < today; ++i)
      {
        var pathToLogFileDirectory = Path.Combine(pathToLogDirectory, $"{directoryPrefix}{i:00}");
        logEntries.AddRange(ReadAllLogFiles(pathToLogFileDirectory));
      }

      return logEntries.OrderBy(le => le.EnteredOn).ToArray();
    }

    private static SimDataLogEntry[] LoadThisYear(Simulation simulation)
    {
      var pathToLogDirectory = simulation.GetPathToLogDirectory();
      if (!Directory.Exists(pathToLogDirectory))
      {
        return Array.Empty<SimDataLogEntry>();
      }
      var logDirectory = new DirectoryInfo(pathToLogDirectory);
      var now = DateTime.Now;
      var logFileDirectories = logDirectory.GetDirectories($"{now.ToString("yyyy", InvariantCulture)}*", SearchOption.TopDirectoryOnly);

      var thisMonthDirectoryPrefix = $"{now.ToString("yyyy-MM", InvariantCulture)}-";
      var logEntries = new List<SimDataLogEntry>();

      foreach (var logFileDirectory in logFileDirectories)
      {
        if (logFileDirectory.Name.StartsWith(thisMonthDirectoryPrefix, StringComparison.InvariantCulture)) continue;

        logEntries.AddRange(ReadAllLogFiles(logFileDirectory.FullName));
      }

      return logEntries.OrderBy(le => le.EnteredOn).ToArray();
    }

    private static SimDataLogEntry[] LoadPreviousYears(Simulation simulation)
    {
      var pathToLogDirectory = simulation.GetPathToLogDirectory();
      if (!Directory.Exists(pathToLogDirectory))
      {
        return Array.Empty<SimDataLogEntry>();
      }
      var logDirectory = new DirectoryInfo(pathToLogDirectory);
      var now = DateTime.Now;
      var logFileDirectories = logDirectory.GetDirectories();

      var thisYearDirectoryPrefix = $"{now.ToString("yyyy", InvariantCulture)}-";
      var logEntries = new List<SimDataLogEntry>();

      foreach (var logFileDirectory in logFileDirectories)
      {
        if (logFileDirectory.Name.StartsWith(thisYearDirectoryPrefix, StringComparison.InvariantCulture)) continue;

        logEntries.AddRange(ReadAllLogFiles(logFileDirectory.FullName));
      }

      return logEntries.OrderBy(le => le.EnteredOn).ToArray();
    }

    private static IEnumerable<SimDataLogEntry> ReadAllLogFiles(string pathToLogFileDirectory)
    {
      var directory = new DirectoryInfo(pathToLogFileDirectory);
      if (!directory.Exists) return Enumerable.Empty<SimDataLogEntry>();

      var logEntries = new List<SimDataLogEntry>();

      var logFiles = directory.GetFiles($"*.{SimExt.LogFileExtension}");
      foreach (var logFile in logFiles)
      {
        try
        {
          var lines = File.ReadAllLines(logFile.FullName);
          logEntries.AddRange(lines.Select(l => SimDataLogEntry.Parse(l)).Somes());
        }
        catch (Exception ex)
        {
          Logger.Log.Error(ex, $"Failed to read log file {logFile.FullName}");
        }
      }

      return logEntries;
    }

    private IReadOnlyCollection<SimDataLogEntry>? _today;
    private IReadOnlyCollection<SimDataLogEntry>? _thisMonth;
    private IReadOnlyCollection<SimDataLogEntry>? _thisYear;
    private IReadOnlyCollection<SimDataLogEntry>? _previousYears;

    private readonly Simulation _simulation;
  }
}
