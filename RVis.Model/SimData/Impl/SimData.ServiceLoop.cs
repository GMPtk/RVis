using LanguageExt;
using RVis.Model.Extensions;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using static RVis.Model.Logger;
using static RVis.Base.Check;

namespace RVis.Model
{
  public sealed partial class SimData
  {
    private void PersistOutputsImpl(CancellationToken cancellationToken)
    {
      while (!cancellationToken.IsCancellationRequested)
      {
        var firstUnpersisted = _outputs
          .Where(kvp => kvp.Value.Persist && kvp.Value.PersistedOn == default)
          .Take(1)
          .ToArray();

        if (!firstUnpersisted.Any()) break;

        var unpersisted = firstUnpersisted.Single().Value;

        DateTime persistedOn;

        try
        {
          unpersisted.Simulation.SaveData(unpersisted.Serie, unpersisted.SerieInput);
          persistedOn = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
          Log.Error(ex);
          persistedOn = DateTime.MaxValue;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var persisted = unpersisted.ToPersisted(persistedOn);
        var inputHash = firstUnpersisted.Single().Key;
        _outputs.TryUpdate(inputHash, persisted, unpersisted);

        Log.Debug($"{nameof(SimData)} persisted {inputHash} output on {persistedOn}");

        if (!_outputRequests.IsEmpty) break;
      }
    }

    private int AcquireOutputsImpl(CancellationToken cancellationToken)
    {
      var snapshot = _outputRequests
        .ToArray()
        .OrderBy(kvp => kvp.Value.RequestedOn);

      var nAwaitingServerLicense = 0;

      foreach (var item in snapshot)
      {
        var simDataItem = item.Value;

        try
        {
          Log.Debug($"{nameof(SimData)} processing {simDataItem.Item.SeriesInput.Hash} output request...");
          var outcome = Process(simDataItem, cancellationToken, out Task<OutputRequest?> outputRequestTask);

          cancellationToken.ThrowIfCancellationRequested();

          if (outcome == ProcessingOutcome.AlreadyAcquired || outcome == ProcessingOutcome.AcquiringData)
          {
            _outputRequests.TryRemove(item.Key, out SimDataItem<OutputRequest> _);
            outputRequestTask.ContinueWith(task =>
            {
              OutputRequest outputRequest;
              if (task.IsFaulted)
              {
                RequireNotNull(task.Exception?.InnerException);

                outputRequest = OutputRequest.Create(
                  simDataItem.Item.SeriesInput,
                  task.Exception.InnerException
                  );
              }
              else
              {
                RequireNotNull(task.Result);
                outputRequest = task.Result;
              }

              var fulfilled = SimDataItem.Create(
                outputRequest,
                simDataItem.Simulation,
                simDataItem.Requester,
                simDataItem.RequestToken,
                simDataItem.RequestedOn,
                DateTime.UtcNow
                );
              _outputRequestsSubject.OnNext(fulfilled);
              Log.Debug($"{nameof(SimData)} fulfilled {simDataItem.Item.SeriesInput.Hash} output request on {fulfilled.FulfilledOn}");
            }, cancellationToken);
          }
          else if (outcome == ProcessingOutcome.NoServerAvailable)
          {
            ++nAwaitingServerLicense;
          }
          else
          {
            throw new InvalidOperationException($"Unhandled processing outcome: {outcome}");
          }
        }
        catch (Exception ex)
        {
          _outputRequests.TryRemove(item.Key, out SimDataItem<OutputRequest> _);
          
          var fulfilled = SimDataItem.Create(
            OutputRequest.Create(simDataItem.Item.SeriesInput, ex),
            simDataItem.Simulation,
            simDataItem.Requester,
            simDataItem.RequestToken,
            simDataItem.RequestedOn,
            DateTime.UtcNow
            );
          _outputRequestsSubject.OnNext(fulfilled);
        }
      }

      return nAwaitingServerLicense;
    }

    private void ServeDataImpl(Object? stateInfo)
    {
      var cancellationToken = RequireInstanceOf<CancellationToken>(stateInfo);

      while (!cancellationToken.IsCancellationRequested)
      {
        if (_outputRequests.IsEmpty)
        {
          Log.Debug("Checking for items to persist");
          PersistOutputsImpl(cancellationToken);
        }

        if (_outputRequests.IsEmpty)
        {
          Log.Debug("Waiting for service activation");
          WaitHandle.WaitAny(
          new[]
          {
            _mreDataService.WaitHandle,
            cancellationToken.WaitHandle
          });
          Log.Debug("Service activated");
        }

        if (cancellationToken.IsCancellationRequested) break;

        int nAwaitingServerLicense = AcquireOutputsImpl(cancellationToken);

        if (0 < nAwaitingServerLicense)
        {
          Log.Debug($"{nameof(SimData)} has {nAwaitingServerLicense} items needing a server. Waiting for a slot...");
          _serverPool.SlotFree.WaitOne();
        }

        _mreDataService.Reset();
      }
    }
  }
}
