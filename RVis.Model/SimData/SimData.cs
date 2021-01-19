using LanguageExt;
using RVis.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using static LanguageExt.Prelude;
using static RVis.Base.Check;
using static RVis.Model.Logger;

namespace RVis.Model
{
  public sealed partial class SimData : ISimData, IDisposable
  {
    public SimData(IRVisServerPool serverPool)
    {
      _serverPool = serverPool;
    }

    public IObservable<SimDataItem<OutputRequest>> OutputRequests =>
      _outputRequestsSubject.AsObservable();
    private readonly ISubject<SimDataItem<OutputRequest>> _outputRequestsSubject =
      Subject.Synchronize(new Subject<SimDataItem<OutputRequest>>());

    public bool HasOutput(string serieInputHash, Simulation simulation) =>
      _outputs.ContainsKey((serieInputHash, simulation));

    public bool HasOutput(SimInput serieInput, Simulation simulation) =>
      HasOutput(serieInput.Hash, simulation);

    public Option<NumDataTable> GetOutput(string serieInputHash, Simulation simulation) =>
      _outputs.TryGetValue((serieInputHash, simulation), out SimDataOutput? output)
        ? Some(output.Serie)
        : None;

    public Option<NumDataTable> GetOutput(SimInput serieInput, Simulation simulation) =>
      GetOutput(serieInput.Hash, simulation);

    public Option<(SimInput SerieInput, OutputOrigin OutputOrigin, bool Persist, DateTime PersistedOn)> GetOutputInfo(string serieInputHash, Simulation simulation) =>
      _outputs.TryGetValue((serieInputHash, simulation), out SimDataOutput? output)
        ? Some((output.SerieInput, output.OutputOrigin, output.Persist, output.PersistedOn))
        : None;

    public Option<(SimInput SerieInput, OutputOrigin OutputOrigin, bool Persist, DateTime PersistedOn)> GetOutputInfo(SimInput serieInput, Simulation simulation) =>
      GetOutputInfo(serieInput.Hash, simulation);

    public bool RequestOutput(Simulation simulation, SimInput seriesInput, object requester, object requestToken, bool persist)
    {
      if (!IsDataServiceRunning) StartDataService();

      var seriesInputHash = seriesInput.Hash;
      var key = (seriesInputHash, simulation, requester, requestToken).GetHashCode();

      var outputRequest = OutputRequest.Create(seriesInput, persist);
      var simDataItem = SimDataItem.Create(outputRequest, simulation, requester, requestToken, DateTime.UtcNow);

      var didAdd = _outputRequests.TryAdd(key, simDataItem);

      if (!didAdd) return false;

      Log.Debug($"{nameof(SimData)} queued {seriesInputHash} output request on {simDataItem.RequestedOn}");
      _mreDataService.Set();

      return true;
    }

    public void Clear(bool includePendingRequests)
    {
      if (includePendingRequests)
      {
        Log.Debug($"{nameof(SimData)} output clear all");
        _outputRequests.Clear();
        _outputs.Clear();
        return;
      }

      Log.Debug($"{nameof(SimData)} output count before clear: {_outputs.Count}");

      var keys = _outputs.Keys;
      var utcCutOff = DateTime.UtcNow.AddMilliseconds(-OUTPUTCLEARWINDOWMS);

      foreach (var key in keys)
      {
        if (!_outputs.TryGetValue(key, out SimDataOutput? simDataOutput)) continue;

        if (simDataOutput.AcquiredOn < utcCutOff)
        {
          _outputs.TryRemove(key, out SimDataOutput _);
        }
      }

      Log.Debug($"{nameof(SimData)} output count after clear: {_outputs.Count} ({OUTPUTCLEARWINDOWMS}ms cut-off)");
    }

    public Option<(int ms, int n)> GetExecutionInterval(Simulation simulation)
    {
      lock (_syncLock)
      {
        if (!_executionIntervals.ContainsKey(simulation))
        {
          var executionInterval = new SimExecutionInterval(simulation);
          executionInterval.Load();
          _executionIntervals.Add(simulation, executionInterval);
        }

        return _executionIntervals[simulation].GetExecutionInterval();
      }
    }

    public void ResetService()
    {
      _outputRequests.Clear();

      var ctsDataService = _ctsDataService;
      _serviceThread = null;
      _ctsDataService = null;

      ctsDataService?.Cancel();
      ctsDataService?.Dispose();

      NotifyObservers(SimDataEvent.ServiceReset);
    }

    private void NotifyObservers(SimDataEvent @event)
    {
      var timeStamp = DateTime.UtcNow;

      void Notify<T>(ISubject<SimDataItem<T>> subject) =>
        subject.OnNext(
          SimDataItem.Create<T>(default, default, this, @event, timeStamp)
          );

      // output request observers
      Notify(_outputRequestsSubject);

      // others...?
    }

    private void StartDataService()
    {
      RequireFalse(IsDataServiceRunning);

      var thread = new Thread(new ParameterizedThreadStart(ServeData))
      {
        IsBackground = true
      };
      _serviceThread = thread;
      _ctsDataService = new CancellationTokenSource();

      thread.Start(_ctsDataService.Token);
    }

    private bool IsDataServiceRunning => _serviceThread?.IsAlive == true;

    private void ServeData(object? stateInfo)
    {
      Log.Debug($"{nameof(SimData)} entering service loop on MTID={Thread.CurrentThread.ManagedThreadId}");

      try
      {
        ServeDataImpl(stateInfo);
      }
      catch (OperationCanceledException)
      {
        Log.Debug($"{nameof(SimData)} got {nameof(OperationCanceledException)}");
      }
      catch (Exception ex)
      {
        _outputRequestsSubject.OnError(ex);
        Log.Error(ex, $"{nameof(SimData)} service loop");
      }

      Log.Debug($"{nameof(SimData)} exiting service loop");
    }

    private const int OUTPUTCLEARWINDOWMS = 1000;

    private readonly IRVisServerPool _serverPool;
    private readonly ConcurrentDictionary<int, SimDataItem<OutputRequest>> _outputRequests =
      new ConcurrentDictionary<int, SimDataItem<OutputRequest>>();
    private readonly ConcurrentDictionary<(string InputHash, Simulation Simulation), SimDataOutput> _outputs =
      new ConcurrentDictionary<(string InputHash, Simulation Simulation), SimDataOutput>();

    private Thread? _serviceThread;
    private CancellationTokenSource? _ctsDataService;
    private readonly ManualResetEventSlim _mreDataService = new ManualResetEventSlim(false);

    private readonly IDictionary<Simulation, SimExecutionInterval> _executionIntervals = new Dictionary<Simulation, SimExecutionInterval>();
    private readonly object _syncLock = new object();
  }
}
