using LanguageExt;
using RVis.Base.Extensions;
using RVis.Model;
using RVis.Model.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using static Estimation.Logger;
using static LanguageExt.Prelude;
using static RVis.Base.Check;
using static System.Double;

using IterationState = System.ValueTuple<
  int,
  LanguageExt.Arr<(string Name, double Value)>,
  LanguageExt.Arr<(string Name, double Value)>
  >;

using IterationUpdateArr = LanguageExt.Arr<(
  int ChainNo,
  LanguageExt.Arr<(
    string Parameter,
    LanguageExt.Arr<(int Iteration, double Value)> Values
    )> Updates
  )>;

using IterationUpdateList = System.Collections.Generic.List<(
  int ChainNo,
  int Iteration,
  LanguageExt.Arr<(string Parameter, double Value)> ParameterValues
  )>;

namespace Estimation
{
  internal sealed class McmcSim : IDisposable
  {
    internal McmcSim(
      EstimationDesign estimationDesign,
      Simulation simulation,
      ISimData simData,
      Arr<ChainState> chainStates
      )
    {
      RequireNotNull(estimationDesign);
      RequireNotNull(simulation);
      RequireNotNull(simData);
      RequireTrue(chainStates.IsEmpty || chainStates.Count == estimationDesign.Chains);

      _estimationDesign = estimationDesign;
      _simulation = simulation;
      _simData = simData;
      ChainStates = chainStates;

      _proposalLoopWaitInterval = _simData
        .GetExecutionInterval(_simulation)
        .Match(t => t.ms * 2, 200);

      _updateBatchThreshold = 2 * estimationDesign.Chains;

      _defaultInput = simulation.SimConfig.SimInput;
      var parameters = _defaultInput.SimParameters;

      _invariants = _estimationDesign.Priors
        .Filter(mp => mp.Distribution.DistributionType == DistributionType.Invariant)
        .Map(mp => parameters.GetParameter(mp.Name).With(mp.Distribution.Mean));

      _targetParameters = _estimationDesign.Priors
        .Filter(mp => mp.Distribution.DistributionType != DistributionType.Invariant)
        .Map(mp => parameters.GetParameter(mp.Name));
    }

    internal IObservable<IterationUpdateArr> IterationUpdates => _iterationUpdatesSubject.AsObservable();
    private readonly Subject<IterationUpdateArr> _iterationUpdatesSubject = new Subject<IterationUpdateArr>();

    internal IObservable<Arr<(ChainState ChainState, Exception? Fault)>> ChainsUpdates => _chainsUpdatesSubject.AsObservable();
    private readonly Subject<Arr<(ChainState ChainState, Exception? Fault)>> _chainsUpdatesSubject = new Subject<Arr<(ChainState ChainState, Exception? Fault)>>();

    internal bool IsIterating => _ctsIteration != default;

    internal Arr<ChainState> ChainStates { get; private set; }

    internal bool Iterate()
    {
      RequireFalse(IsIterating);
      RequireTrue(_iterationUpdateLst.IsEmpty());

      Arr<McmcChain> chains;

      if (ChainStates.IsEmpty)
      {
        var parameters = _estimationDesign.Priors
          .Filter(mp => mp.Distribution.DistributionType != DistributionType.Invariant);

        chains = Range(1, _estimationDesign.Chains)
          .Map(no => new McmcChain(
            no,
            _estimationDesign.Iterations,
            _estimationDesign.BurnIn,
            _estimationDesign.TargetAcceptRate ?? NaN,
            _estimationDesign.UseApproximation,
            parameters,
            _estimationDesign.Outputs,
            _estimationDesign.Observations
            ))
          .ToArr();
      }
      else
      {
        chains = ChainStates
          .Filter(cs => cs.GetCompletedIterations() < _estimationDesign.Iterations)
          .Map(cs => new McmcChain(
            cs.No,
            _estimationDesign.Iterations,
            _estimationDesign.BurnIn,
            _estimationDesign.TargetAcceptRate ?? NaN,
            _estimationDesign.UseApproximation,
            cs.ModelParameters,
            cs.ModelOutputs,
            _estimationDesign.Observations,
            cs.ChainData,
            cs.ErrorData,
            cs.PosteriorData
            ));
      }

      if (chains.IsEmpty) return false;

      _outputRequestSubscription = _simData.OutputRequests.Subscribe(ObserveOutputRequest);
      _ctsIteration = new CancellationTokenSource();

      try
      {
        foreach (var chain in chains)
        {
          chain.Propose(out IterationState iterationState);

          var (currentIteration, currentValues, proposedValues) = iterationState;

          var input = GetProposalInput(proposedValues);
          _simData.RequestOutput(_simulation, input, this, chain.No, persist: false);

          AddChainDataUpdate(chain.No, currentIteration, currentValues);
        }

        Task.Run(() => ProposalLoopAsync(chains), _ctsIteration.Token);
      }
      catch (Exception)
      {
        Cleanup();
        chains.Iter(c => c.Dispose());
        throw;
      }

      return true;
    }

    private async Task ProposalLoopAsync(Arr<McmcChain> chains)
    {
      RequireNotNull(_ctsIteration);

      try
      {
        var token = _ctsIteration.Token;

        while (true) // chain(s) incomplete and not cancelled
        {
          if (token.IsCancellationRequested) break;

          while (!_outputRequestQueue.IsEmpty) // there is model output to pass on to chain(s)
          {
            var didDequeue = _outputRequestQueue.TryDequeue(out SimDataItem<OutputRequest>? outputRequest);
            if (!didDequeue) break;

            var no = RequireInstanceOf<int>(outputRequest!.RequestToken);
            var chain = chains.Find(c => c.No == no).AssertSome();

            // no R subsystem failure while processing proposal
            void RequestSucceeded(Arr<SimInput> _)
            {
              var output = _simData.GetOutput(outputRequest.Item.SeriesInput, _simulation).AssertSome();
              var independentData = _simulation.SimConfig.SimOutput.GetIndependentData(output);
              var outputData = _estimationDesign.Outputs.Map(mo => output[mo.Name]);

              var didPropose = chain.Propose(independentData, outputData, out IterationState iterationState);
              if (chain.IsFaulted) return;

              var (currentIteration, currentValues, proposedValues) = iterationState;

              if (didPropose)
              {
                var input = GetProposalInput(proposedValues);
                _simData.RequestOutput(_simulation, input, this, chain.No, persist: false);
              }
              else
              {
                RequireTrue(chain.IsComplete);
              }

              AddChainDataUpdate(chain.No, currentIteration, currentValues);
            }

            // system failure or R choked on proposal
            void RequestFailed(Exception e) => chain.FaultChain(e);

            outputRequest.Item.SerieInputs.Match(RequestSucceeded, RequestFailed);
          }

          var isFinished = chains.ForAll(c => c.IsComplete);
          if (isFinished) break;

          // give data a chance to accumulate
          await Task.Delay(_proposalLoopWaitInterval);

          // wait for any data; release after timeout to check for cancellation
          _outputRequestEvent.WaitOne(_proposalLoopWaitInterval);
        }

        // alert client of any remaining chain data
        PublishIterationUpdate();
      }
      catch (Exception ex)
      {
        Log.Error(ex, "Acquire");
      }

      try
      {
        // forward acquired data to client
        var chainsUpdate = chains.Map(c =>
        {
          var (modelParameters, modelOutputs, chainData, errorData, posteriorData) = c.GetState();

          return (
            ChainState: new ChainState(c.No, modelParameters, modelOutputs, chainData, errorData, posteriorData),
            c.Fault
            );
        });

        if (ChainStates.IsEmpty)
        {
          ChainStates = chainsUpdate.Map(cu => cu.ChainState);
        }
        else
        {
          ChainStates = ChainStates.Map(
            e => chainsUpdate
              .Find(cu => cu.ChainState.No == e.No)
              .Match(cu => cu.ChainState, () => e)
            );
        }

        _chainsUpdatesSubject.OnNext(chainsUpdate);
      }
      catch (Exception ex)
      {
        Log.Error(ex, "Publish");
        _chainsUpdatesSubject.OnError(ex);
      }

      Cleanup();
      chains.Iter(c => c.Dispose());
    }

    private void Cleanup()
    {
      _ctsIteration?.Dispose();
      _ctsIteration = default;

      _outputRequestSubscription?.Dispose();
      _outputRequestSubscription = default;
    }

    internal void StopIterating()
    {
      _ctsIteration?.Cancel();
    }

    public void Dispose() => Dispose(true);

    private void ObserveOutputRequest(SimDataItem<OutputRequest> outputRequest)
    {
      if (outputRequest.Requester != this) return;

      _outputRequestQueue.Enqueue(outputRequest);
      _outputRequestEvent.Set();
    }

    private void Dispose(bool disposing)
    {
      if (!_disposed)
      {
        if (disposing)
        {
          try
          {
            _ctsIteration?.Cancel();
            Cleanup();

            _outputRequestEvent.Dispose();

            _chainsUpdatesSubject.Dispose();
            _iterationUpdatesSubject.Dispose();
          }
          catch (Exception ex)
          {
            Log.Error(ex);
          }
        }

        _disposed = true;
      }
    }

    private void AddChainDataUpdate(int chainNo, int iteration, Arr<(string Name, double Value)> parameterValues)
    {
      if (parameterValues.IsEmpty) return;

      _iterationUpdateLst.Add((chainNo, iteration, parameterValues));

      if (_iterationUpdateLst.Count == _updateBatchThreshold)
      {
        PublishIterationUpdate();
      }
    }

    private void PublishIterationUpdate()
    {
      if (_iterationUpdateLst.IsEmpty()) return;

      var byChain = _iterationUpdateLst
        .GroupBy(u => u.ChainNo)
        .OrderBy(g => g.Key)
        .ToArr();

      _iterationUpdateLst.Clear();

      var perChainUpdate = new SortedDictionary<string, List<(int Iteration, double Value)>>();

      var iterationUpdate = byChain.Map(g =>
      {
        perChainUpdate.Clear();

        foreach (var (_, iteration, current) in g)
        {
          current.Iter(pv =>
          {
            var didContain = perChainUpdate.TryGetValue(
              pv.Parameter,
              out List<(int Iteration, double Value)>? chainData
              );

            if (!didContain)
            {
              chainData = new List<(int Iteration, double Value)>();
              perChainUpdate.Add(pv.Parameter, chainData);
            }

            chainData!.Add((iteration, pv.Value));
          });
        }

        return (
          ChainNo: g.Key,
          Updates: perChainUpdate
            .Select(kvp => (Parameter: kvp.Key, Values: kvp.Value.ToArr()))
            .ToArr()
          );
      });

      _iterationUpdatesSubject.OnNext(iterationUpdate);
    }

    private SimInput GetProposalInput(Arr<(string Name, double Value)> proposalValues) =>
      _defaultInput.With(_targetParameters.Map(p => p.With(proposalValues.Snd(p.Name))) + _invariants);

    private readonly EstimationDesign _estimationDesign;
    private readonly Simulation _simulation;
    private readonly ISimData _simData;
    private readonly SimInput _defaultInput;
    private readonly Arr<SimParameter> _invariants;
    private readonly Arr<SimParameter> _targetParameters;
    private readonly int _proposalLoopWaitInterval;
    private readonly int _updateBatchThreshold;
    private readonly ConcurrentQueue<SimDataItem<OutputRequest>> _outputRequestQueue =
      new ConcurrentQueue<SimDataItem<OutputRequest>>();
    private readonly AutoResetEvent _outputRequestEvent = new AutoResetEvent(false);
    private CancellationTokenSource? _ctsIteration;
    private IDisposable? _outputRequestSubscription;
    private readonly IterationUpdateList _iterationUpdateLst = new IterationUpdateList();
    private bool _disposed = false;
  }
}
