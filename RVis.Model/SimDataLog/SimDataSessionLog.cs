using LanguageExt;
using RVis.Base.Extensions;
using RVis.Model.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using static LanguageExt.Prelude;
using static RVis.Base.Check;

namespace RVis.Model
{
  public sealed class SimDataSessionLog : IDisposable
  {
    public SimDataSessionLog(SimData simData, IObservable<long> secondInterval)
    {
      _simData = simData;
      _secondInterval = secondInterval;
    }

    public IObservable<SimDataLogEntry> LogEntries =>
      _logEntriesSubject.AsObservable();
    private readonly ISubject<SimDataLogEntry> _logEntriesSubject =
      new Subject<SimDataLogEntry>();

    public Arr<SimDataLogEntry> Log
    {
      get
      {
        lock (_logEntriesSyncLock)
        {
          return _logEntries.ToArr();
        }
      }
    }

    public void StartSession(Simulation simulation)
    {
      RequireTrue(_logEntries.Count == 0);

      _logIndex = 0;

      var pathToSessionLog = simulation.GetPathToSessionLog();

      if (File.Exists(pathToSessionLog))
      {
        var lines = File.ReadAllLines(pathToSessionLog);
        var logEntries = lines.Select(l => SimDataLogEntry.Parse(l)).Somes();
        lock (_logEntriesSyncLock)
        {
          _logEntries.AddRange(logEntries);
          _logIndex = logEntries.Max(le => le.ID) + 1;
        }
      }

      _secondIntervalSubscription = _secondInterval.Subscribe(ObserveSecondInterval);
      _outputRequestSubscription = _simData.OutputRequests.Subscribe(ObserveOutputRequest);
    }

    public void EndSession()
    {
      _secondIntervalSubscription?.Dispose();
      _secondIntervalSubscription = default;
      _outputRequestSubscription?.Dispose();
      _outputRequestSubscription = default;

      if (HaveOngoingWriteLogEntriesTask) _writeLogEntriesTask!.Wait();
      _writeLogEntriesTask = default;
      if (!_pendingToFile.IsEmpty) WritePendingToFile();

      lock (_logEntriesSyncLock)
      {
        _logEntries.Clear();
      }
    }

    public void Dispose() => Dispose(true);

    private void Dispose(bool disposing)
    {
      if (!_disposed)
      {
        if (disposing)
        {
          if (_logEntriesSubject is IDisposable logEntriesSubject) logEntriesSubject.Dispose();

          _secondIntervalSubscription?.Dispose();
          _outputRequestSubscription?.Dispose();

          if (HaveOngoingWriteLogEntriesTask) _writeLogEntriesTask!.Wait();

          if (!_pendingToFile.IsEmpty) WritePendingToFile();
        }

        _disposed = true;
      }
    }

    private void ObserveOutputRequest(SimDataItem<OutputRequest> simDataItem)
    {
      if (simDataItem.IsResetEvent()) return;

      simDataItem.Item.SerieInputs.IfRight(
        o => LogOutputRequest(simDataItem, o)
        );
    }

    private void LogOutputRequest(SimDataItem<OutputRequest> simDataItem, Arr<SimInput> serieInputs)
    {
      var requesterTypeName = simDataItem.Requester.GetType().Name;
      var description = simDataItem.GetDescription();
      var simulation = simDataItem.Simulation;
      var defaultInput = simulation.SimConfig.SimInput;

      var logging = serieInputs
        .Map(i =>
        {
          var outputInfo = _simData.GetOutputInfo(i, simulation);
          return outputInfo.Match(
            oi => Some((Input: i, oi.SerieInput.Hash, oi.Persist)),
            () => None
            );
        })
        .Somes()
        .Map(t => new
        {
          LogEntry = new SimDataLogEntry(
            ++_logIndex,
            DateTime.UtcNow,
            t.Hash,
            defaultInput.GetEdits(t.Input).ToAssignments(),
            requesterTypeName,
            description
            ),
          t.Persist
        })
        .ToArr();

      logging
        .Filter(l => l.Persist)
        .Iter(l => _pendingToFile.Enqueue((simulation, l.LogEntry)));

      var sessionLogIsRequester = requesterTypeName == ToString();

      if (!sessionLogIsRequester)
      {
        var logEntries = logging.Map(l => l.LogEntry);

        lock (_logEntriesSyncLock)
        {
          _logEntries.AddRange(logEntries);
        }

        logEntries.Iter(le => _logEntriesSubject.OnNext(le));
      }
    }

    private void ObserveSecondInterval(long _)
    {
      PersistPendingLogEntries();
    }

    private void PersistPendingLogEntries()
    {
      if (HaveOngoingWriteLogEntriesTask) return;

      _writeLogEntriesTask = null;

      if (_pendingToFile.IsEmpty) return;

      _writeLogEntriesTask = Task.Run(WritePendingToFile);
    }

    private void WritePendingToFile()
    {
      try
      {
        if (!_pendingToFile.TryDequeue(out (Simulation Simulation, SimDataLogEntry LogEntry) pending)) return;

        var logEntries = new List<SimDataLogEntry>
        {
          pending.LogEntry
        };

        var simulation = pending.Simulation;
        var pathToSessionLog = simulation.GetPathToSessionLog();
        var pathToSessionLogDirectory = Path.GetDirectoryName(pathToSessionLog);
        RequireNotNull(pathToSessionLogDirectory);
        if (!Directory.Exists(pathToSessionLogDirectory)) Directory.CreateDirectory(pathToSessionLogDirectory);

        using var file = new StreamWriter(pathToSessionLog, true);
        
        while (_pendingToFile.TryDequeue(out pending))
        {
          RequireTrue(simulation == pending.Simulation);
          logEntries.Add(pending.LogEntry);
        }
        
        foreach (var logEntry in logEntries) file.WriteLine(logEntry.ToString());
      }
      catch (Exception ex)
      {
        Logger.Log.Error(ex, $"{nameof(SimDataSessionLog)}::{nameof(WritePendingToFile)}");
      }
    }

    private bool HaveOngoingWriteLogEntriesTask =>
       null != _writeLogEntriesTask &&
       _writeLogEntriesTask.IsCompleted != true &&
       _writeLogEntriesTask.IsFaulted != true;

    private readonly SimData _simData;
    private readonly IObservable<long> _secondInterval;

    private IDisposable? _secondIntervalSubscription;
    private IDisposable? _outputRequestSubscription;

    private int _logIndex;
    private readonly List<SimDataLogEntry> _logEntries = new List<SimDataLogEntry>();
    private readonly object _logEntriesSyncLock = new object();
    private readonly ConcurrentQueue<(Simulation Simulation, SimDataLogEntry LogEntry)> _pendingToFile =
      new ConcurrentQueue<(Simulation Simulation, SimDataLogEntry LogEntry)>();
    private Task? _writeLogEntriesTask;

    private bool _disposed = false;
  }
}
