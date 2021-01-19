using LanguageExt;
using RVis.Base.Extensions;
using RVis.Model;
using RVis.Model.Extensions;
using System;
using System.Data;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using static LanguageExt.Prelude;
using static RVis.Base.Check;
using static Sensitivity.Method;

namespace Sensitivity
{
  internal sealed partial class DesignViewModel
  {
    private async Task CreateFast99DesignAsync(
      Arr<(string Name, IDistribution Distribution)> parameterDistributions,
      IRVisClient client
      )
    {
      RequireTrue(NoOfSamples > 0);

      var (samples, serializedDesign) = await GetFast99SamplesAsync(
        parameterDistributions,
        NoOfSamples.Value,
        client
        );

      var sensitivityDesign = SensitivityDesign.CreateSensitivityDesign(
        DateTime.Now,
        Array(serializedDesign),
        parameterDistributions,
        SensitivityMethod.Fast99,
        Array((nameof(NoOfSamples), NoOfSamples.Value as object)).FromMethodParameters(),
        Array(samples)
        );
      _sensitivityDesigns.Add(sensitivityDesign);

      UnloadCurrentDesign();

      _moduleState.SensitivityDesign = default;

      _moduleState.Ranking = default;
      _moduleState.Trace = default;
      _moduleState.MeasuresState.MorrisOutputMeasures = default;
      _moduleState.MeasuresState.Fast99OutputMeasures = default;
      _moduleState.SensitivityDesign = sensitivityDesign;

      if (_moduleState.MeasuresState.SelectedOutputName.IsntAString())
      {
        var firstDependentVariable = _simulation.SimConfig.SimOutput.DependentVariables.First();
        _moduleState.MeasuresState.SelectedOutputName = firstDependentVariable.Name;
      }

      _sampleInputs = CompileSampleInputs(
        _simulation,
        Array(samples),
        sensitivityDesign.DesignParameters.Filter(
          dp => dp.Distribution.DistributionType == DistributionType.Invariant
          )
        );

      var outputRequestJob = CompileOutputRequestJob(
        _moduleState.MeasuresState.SelectedOutputName,
        _simulation,
        _simData,
        _sampleInputs
        );

      if (!outputRequestJob[0].Output.IsEmpty)
      {
        var trace = _simData
          .GetOutput(outputRequestJob[0].Input, _simulation)
          .AssertSome();
        _moduleState.Trace = trace;
        _sensitivityDesigns.SaveTrace(_moduleState.SensitivityDesign, trace);
      }

      NOutputsAcquired = outputRequestJob.Count(t => t.Input == default || t.Output != default);
      NOutputsToAcquire = samples.Rows.Count;
      if (NOutputsAcquired < NOutputsToAcquire) _outputRequestJob = outputRequestJob;

      var inputs = CreateInputs(Array(samples));
      ShowIssues = false;
      UpdateInputs(inputs, outputRequestJob);

      DesignCreatedOn = sensitivityDesign.CreatedOn;

      var designOutputs = outputRequestJob
        .Select(t => t.Output)
        .ToArr();

      var canMeasure = designOutputs.ForAll(o => o != default);

      if (canMeasure) Measure(designOutputs);
    }

    private async Task GenerateFast99OutputMeasuresAsync(
      string outputName,
      Arr<Arr<double>> designOutputs,
      ServerLicense serverLicense
      )
    {
      using (serverLicense)
      {
        RequireNotNull(_moduleState.SensitivityDesign);
        RequireNotNull(_moduleState.Trace);

        _cancellationTokenSource = new CancellationTokenSource();

        TaskName = "Generate e-FAST Output Measures";
        IsRunningTask = true;
        CanCancelTask = true;

        try
        {
          var measures = await Method.GenerateFast99OutputMeasuresAsync(
            _moduleState.SensitivityDesign.SerializedDesigns.Single(),
            _moduleState.SensitivityDesign.Samples.Single(),
            designOutputs,
            _simulation.SimConfig.SimOutput.GetIndependentData(_moduleState.Trace),
            await serverLicense.GetRClientAsync(),
            s => _appService.ScheduleLowPriorityAction(() => RaiseTaskMessageEvent(s)),
            _cancellationTokenSource.Token
          );

          _moduleState.MeasuresState.SelectedOutputName = outputName;
          _moduleState.MeasuresState.Fast99OutputMeasures =
            _moduleState.MeasuresState.Fast99OutputMeasures.Add(outputName, measures);

          UpdateDesignEnable();
          UpdateSamplesEnable();

          _sensitivityDesigns.SaveFast99OutputMeasures(
            _moduleState.SensitivityDesign,
            outputName,
            measures.FirstOrder,
            measures.TotalOrder,
            measures.Variance
            );
        }
        catch (OperationCanceledException)
        {
          // expected
        }
        catch (Exception ex)
        {
          _appService.Notify(
            nameof(DesignViewModel),
            nameof(GenerateFast99OutputMeasuresAsync),
            ex
            );
        }

        IsRunningTask = false;
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = default;
      }
    }
  }
}
