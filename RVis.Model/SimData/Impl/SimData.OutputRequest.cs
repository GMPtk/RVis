using LanguageExt;
using RVis.Base.Extensions;
using RVis.Data;
using RVis.Model.Extensions;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using static LanguageExt.Prelude;
using static RVis.Model.Logger;
using static System.Double;
using static System.String;

namespace RVis.Model
{
  public sealed partial class SimData
  {
    private static Arr<SimInput> EvaluateNonScalars(SimInput seriesInput, SimInput defaultInput, IRVisClient client)
    {
      var nonScalarParameters = seriesInput.SimParameters.Filter(p =>
      {
        var parameter = defaultInput.SimParameters.GetParameter(p.Name);
        var isNonScalarEdit = p.Value != parameter.Value && IsNaN(p.Scalar);
        return isNonScalarEdit;
      });

      if (nonScalarParameters.IsEmpty) return Array(seriesInput);

      var evaluations = nonScalarParameters.Map(
        p => new
        {
          Parameter = p,
          Evaluation = client.EvaluateNumData(p.Value)
        });

      var invalid = evaluations.Filter(a => a.Evaluation.Length != 1);
      if (!invalid.IsEmpty)
        throw new InvalidOperationException(
          "Parameter values failed to produce single-column output: " +
          Join(", ", invalid.Map(e => e.Parameter.Name))
          );

      var singleColumns = evaluations
        .Map(e => new { e.Parameter, Sequence = e.Evaluation[0].Data });
      var nParameters = singleColumns.Count;
      var maxSequenceLength = singleColumns.Max(a => a.Sequence.Count);
      var serieInputs = Range(0, maxSequenceLength).Map(i =>
      {
        var parameters = singleColumns.Map(
          a => a.Parameter.With(a.Sequence[i % a.Sequence.Count])
          );
        return seriesInput.With(parameters);
      });

      return serieInputs.ToArr();
    }

    private enum ProcessingOutcome
    {
      AcquiringData,
      NoServerAvailable,
      AlreadyAcquired
    }

    private OutputRequest FulfilRequest(Simulation simulation, SimInput serieInput)
    {
      Log.Debug($"{nameof(SimData)} retrieving output");

      var serie = simulation.LoadData(serieInput);

      _outputs.TryAdd(
        (serieInput.Hash, simulation),
        SimDataOutput.Create(simulation, serieInput, serie, OutputOrigin.Storage, false)
        );

      return OutputRequest.Create(serieInput, Array(serieInput));
    }

    private OutputRequest FulfilRequest(Simulation simulation, SimInput seriesInput, IRVisClient client, bool persist)
    {
      var serieInputs = EvaluateNonScalars(
        seriesInput,
        simulation.SimConfig.SimInput,
        client
        );

      Log.Debug($"Evaluating non-scalar parameter values produced n={serieInputs.Count} series");

      var useExec = simulation.SimConfig.SimCode.Exec.IsAString();

      (SimInput SerieInput, NumDataTable Serie, bool Persist, OutputOrigin OutputOrigin) AcquireSerieData(SimInput serieInput)
      {
        if (simulation.HasData(serieInput))
        {
          Log.Debug($"{nameof(SimData)} retrieving data");
          var retrieved = simulation.LoadData(serieInput);
          return (serieInput, retrieved, false, OutputOrigin.Storage);
        }

        Log.Debug($"{nameof(SimData)} generating data");

        var serieConfig = simulation.SimConfig.With(serieInput);
        NumDataTable serie;

        var stopWatch = Stopwatch.StartNew();

        if (useExec)
        {
          client.RunExec(simulation.PathToCodeFile, serieConfig);
          serie = client.TabulateExecOutput(serieConfig);
        }
        else
        {
          var pathToCodeFile = simulation.PopulateTemplate(serieConfig.SimInput.SimParameters);
          client.SourceFile(pathToCodeFile);
          serie = client.TabulateTmplOutput(serieConfig);
        }

        stopWatch.Stop();
        var msElapsed = 1000L * stopWatch.ElapsedTicks / Stopwatch.Frequency;

        lock (_syncLock)
        {
          if (!_executionIntervals.TryGetValue(simulation, out SimExecutionInterval executionInterval))
          {
            executionInterval = new SimExecutionInterval(simulation);
            executionInterval.Load();
            _executionIntervals.Add(simulation, executionInterval);
          }

          executionInterval.AddInterval(msElapsed);
        }

        return (serieInput, serie, persist, OutputOrigin.Generation);
      }

      var outputs = serieInputs.Map(AcquireSerieData);

      var outputsRequiringPersistence = outputs
        .Filter(t =>
        {
          var didAdd = _outputs.TryAdd(
            (t.SerieInput.Hash, simulation),
            SimDataOutput.Create(simulation, t.SerieInput, t.Serie, t.OutputOrigin, t.Persist)
            );

          return didAdd && t.Persist;
        });

      if (!outputsRequiringPersistence.IsEmpty) _mreDataService.Set();

      return OutputRequest.Create(
        seriesInput,
        outputs.Map(t => t.SerieInput)
        );
    }

    private ProcessingOutcome Process(
      SimDataItem<OutputRequest> simDataItem,
      CancellationToken cancellationToken,
      out Task<OutputRequest> outputRequestTask
      )
    {
      var simulation = simDataItem.Simulation;
      var seriesInput = simDataItem.Item.SeriesInput;

      ProcessingOutcome processingOutcome;

      if (_outputs.TryGetValue((seriesInput.Hash, simulation), out SimDataOutput _))
      {
        outputRequestTask = Task.FromResult(OutputRequest.Create(seriesInput, Array(seriesInput)));
        processingOutcome = ProcessingOutcome.AlreadyAcquired;
      }
      else if (simulation.HasData(seriesInput))
      {
        outputRequestTask = Task.Run(
          () => FulfilRequest(simulation, seriesInput),
          cancellationToken
          );
        processingOutcome = ProcessingOutcome.AcquiringData;
      }
      else
      {
        Task<OutputRequest> SomeServerLicense(ServerLicense serverLicense) =>
          Task.Run(() =>
          {
            using (serverLicense)
            {
              return FulfilRequest(
                simulation,
                seriesInput,
                serverLicense.Client,
                simDataItem.Item.Persist
                );
            }
          }, cancellationToken);

        Task<OutputRequest> NoServerLicense() =>
          Task.FromResult(default(OutputRequest));

        _serverPool.SlotFree.WaitOne(50);

        var maybeServerLicense = _serverPool.RequestServer();

        outputRequestTask = maybeServerLicense.Match(
          SomeServerLicense,
          NoServerLicense
          );

        processingOutcome =
          maybeServerLicense.IsSome ?
          ProcessingOutcome.AcquiringData :
          ProcessingOutcome.NoServerAvailable;
      }

      return processingOutcome;
    }
  }
}
