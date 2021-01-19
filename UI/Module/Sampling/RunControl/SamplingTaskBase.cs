using LanguageExt;
using RVis.Base.Extensions;
using RVis.Data.Extensions;
using RVis.Model;
using RVis.Model.Extensions;
using RVisUI.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using static RVis.Base.Check;
using static RVis.Data.FxData;
using static Sampling.Logger;
using static System.Globalization.CultureInfo;
using static System.IO.Path;
using FxDataColumn = System.Data.DataColumn;
using FxDataTable = System.Data.DataTable;
using RvDataTable = RVis.Data.NumDataTable;

namespace Sampling
{
  internal abstract class SamplingTaskBase : IRunControlTask
  {
    public SamplingTaskBase(
      string name,
      Arr<(string Parameter, Arr<double> Correlations)> correlation,
      Arr<string> outputNames,
      IAppState appState,
      IAppService appService
      )
    {
      Name = name;
      Correlation = correlation;
      OutputNames = outputNames;
      _appState = appState;
      _appService = appService;
    }

    public string Name { get; }
    public Arr<(string Parameter, Arr<double> Correlations)> Correlation { get; }
    public Arr<string> OutputNames { get; }

    protected abstract Task<FxDataTable> GetSamplesAsync(
      RunControlConfiguration configuration,
      Simulation simulation,
      string pathToWorkingDirectory,
      ISubject<(DateTime Timestamp, string Message)> messages,
      CancellationToken cancellationToken
      );

    public async Task RunAsync(
      RunControlConfiguration configuration,
      Simulation simulation,
      string pathToWorkingDirectory,
      ISubject<(DateTime Timestamp, string Message)> messages,
      CancellationToken cancellationToken
      )
    {
      var unknownOutputs = OutputNames
        .Map(on => (Name: on, Element: simulation.SimConfig.SimOutput.FindElement(on)))
        .Filter(t => t.Element.IsNone)
        .Map(t => t.Name)
        .ToArr();
      RequireTrue(
        unknownOutputs.IsEmpty,
        $"Found non-existent output name in task \"{Name}\": {string.Join(", ", unknownOutputs)}"
        );

      var samplesTable = await GetSamplesAsync(
        configuration,
        simulation,
        pathToWorkingDirectory,
        messages,
        cancellationToken
        );

      cancellationToken.ThrowIfCancellationRequested();

      var simInput = simulation.SimConfig.SimInput;
      var parameters = samplesTable.Columns
        .Cast<FxDataColumn>()
        .Select(
          dc => simInput.SimParameters
            .FindParameter(dc.ColumnName)
            .AssertSome($"Non-existent parameter specified in task \"{Name}\": {dc.ColumnName}")
        )
        .ToArr();

      var NSamples = samplesTable.Rows.Count;
      var pathToOutputsDirectory = Combine(pathToWorkingDirectory, "outputs");
      var independentVariable = simulation.SimConfig.SimOutput.IndependentVariable;

      double[]? independentData = null;
      var pathToIndependentData = Combine(
        pathToOutputsDirectory,
        $"{independentVariable.Name.ToValidFileName()}.txt"
        );
      if (File.Exists(pathToIndependentData))
      {
        var lines = await File.ReadAllLinesAsync(pathToIndependentData, cancellationToken);
        independentData = lines
          .Select(l => double.TryParse(l, out double d)
            ? d
            : throw new InvalidOperationException($"Found invalid double in independent data: {l}")
            )
          .ToArray();
      }

      var inputs = new Queue<(int SampleNo, SimInput Input)>(NSamples);
      var fxOutputs = new Dictionary<int, FxDataTable>(NSamples);
      var nZeroes = Convert.ToInt32(Math.Truncate(Math.Log10(NSamples)) + 1);
      var fmtCSV = new string('0', nZeroes);

      for (var i = 0; i < NSamples; ++i)
      {
        var pathToOutput = Combine(pathToOutputsDirectory, $"{(i + 1).ToString(fmtCSV)}.csv");
        if (File.Exists(pathToOutput))
        {
          var outputTable = LoadFromCSV<double>(pathToOutput);
          fxOutputs.Add(i + 1, outputTable);
        }
        else
        {
          var values = samplesTable.Rows[i].ItemArray.Cast<double>().ToArr();
          var requestInput = simInput.With(
            parameters.Map((j, p) => p.With(values[j])).ToArr()
            );
          inputs.Enqueue((i + 1, requestInput));
        }
      }

      if (!Directory.Exists(pathToOutputsDirectory))
      {
        Directory.CreateDirectory(pathToOutputsDirectory);
      }

      var rvOutputs = new Dictionary<int, RvDataTable>(NSamples);
      var nInProgress = 0;
      var nProcessed = fxOutputs.Count;
      var semaphoreSlim = new SemaphoreSlim(1, 1);
      var timeStamp = DateTime.Now;

      void Left(Exception ex)
      {
        Log.Error(ex);
      }

      void Right(int sampleNo, Arr<SimInput> serieInputs)
      {
        if (serieInputs.Count != 1)
        {
          Log.Error($"(Job {configuration.Title}/Task {Name}) Unexpected output for sample no. {sampleNo}");
        }

        var output = _appState.SimData
          .GetOutput(serieInputs[0], simulation)
          .AssertSome("Internal error (missing SimData output)");

        rvOutputs.Add(sampleNo, output);
      }

      void ObserveOutputRequests(SimDataItem<OutputRequest> outputRequest)
      {
        if (cancellationToken.IsCancellationRequested) return;

        if (outputRequest.Requester != this) return;

        semaphoreSlim.Wait(cancellationToken);

        try
        {
          outputRequest.Item.SerieInputs.Match(
            sis => Right(RequireInstanceOf<int>(outputRequest.RequestToken), sis),
            Left
          );

          ++nProcessed;
          --nInProgress;

          messages.OnNext((timeStamp, $"Outputs generated: {nProcessed} / {NSamples}"));

          while (inputs.Count > 0 && nInProgress < configuration.MaxCores)
          {
            var (sampleNo, input) = inputs.Dequeue();

            ++nInProgress;

            if (!_appState.SimData.RequestOutput(simulation, input, this, sampleNo, false))
            {
              ++nProcessed;
              --nInProgress;
            }
          }
        }
        finally
        {
          semaphoreSlim.Release();
        }
      }

      var subscription = _appState.SimData.OutputRequests.Subscribe(ObserveOutputRequests);

      try
      {
        messages.OnNext((timeStamp, $"Outputs generated: {nProcessed} / {NSamples}"));

        if (inputs.Count > 0)
        {
          var (sampleNo, input) = inputs.Dequeue();
          if (!_appState.SimData.RequestOutput(simulation, input, this, sampleNo, false))
          {
            messages.OnNext((DateTime.Now, "Failed to queue SimData request"));
            return;
          }
          ++nInProgress;
        }

        while (true)
        {
          await Task.Delay(50, cancellationToken);
          cancellationToken.ThrowIfCancellationRequested();
          await semaphoreSlim.WaitAsync(cancellationToken);

          try
          {
            while (rvOutputs.Count > 0)
            {
              var (sampleNo, rvOutput) = rvOutputs.First();
              rvOutputs.Remove(sampleNo);
              if (independentData is null)
              {
                independentData = simulation.SimConfig.SimOutput.GetIndependentData(rvOutput).Data.ToArray();
                var lines = independentData.Select(d => d.ToString(InvariantCulture));
                await File.WriteAllLinesAsync(pathToIndependentData, lines, cancellationToken);
              }
              var numDataColumns = OutputNames.Map(on => rvOutput[on]);
              rvOutput = new RvDataTable(rvOutput.Name, numDataColumns);
              var fxOutput = rvOutput.ToFxDataTable();
              fxOutputs.Add(sampleNo, fxOutput);
              var pathToOutput = Combine(pathToOutputsDirectory, $"{sampleNo.ToString(fmtCSV)}.csv");
              SaveToCSV<double>(fxOutput, pathToOutput);
            }

            if (nProcessed == NSamples)
            {
              RequireTrue(inputs.Count == 0);
              RequireTrue(nInProgress == 0);
              break;
            }
          }
          finally
          {
            semaphoreSlim.Release();
          }
        }
      }
      finally
      {
        subscription.Dispose();
        semaphoreSlim.Dispose();
      }

      cancellationToken.ThrowIfCancellationRequested();

      messages.OnNext((DateTime.Now, "Generation complete"));

      if (independentData is null || independentData.Length == 0)
      {
        messages.OnNext((DateTime.Now, "Cannot compile outputs (no independent data available)"));
        return;
      }

      if (fxOutputs.Any(kvp => kvp.Value.Rows.Count != independentData.Length))
      {
        messages.OnNext((DateTime.Now, "Cannot compile outputs (varying output lengths encountered)"));
        return;
      }

      CompilePerOutput(
        messages,
        fxOutputs,
        independentData,
        pathToWorkingDirectory,
        cancellationToken
        );
    }

    private void CompilePerOutput(
      ISubject<(DateTime Timestamp, string Message)> messages,
      Dictionary<int, FxDataTable> fxOutputs,
      double[] independentData,
      string pathToWorkingDirectory,
      CancellationToken cancellationToken
      )
    {
      void CompileOutput(string outputName)
      {
        var fxOutput = new FxDataTable();
        fxOutput.Columns.Add(new FxDataColumn("Spl No.", typeof(int)));
        for (var i = 0; i < independentData.Length; ++i)
        {
          fxOutput.Columns.Add(new FxDataColumn($"Timepoint No.{i + 1}", typeof(double)));
        }

        var itemArray = new object[independentData.Length + 1];
        for (var i = 0; i < independentData.Length; ++i)
        {
          itemArray[i + 1] = independentData[i];
        }
        fxOutput.Rows.Add(itemArray);

        foreach (var (sampleNo, sampleOutput) in fxOutputs)
        {
          itemArray[0] = sampleNo;
          for (var i = 0; i < independentData.Length; ++i)
          {
            itemArray[i + 1] = sampleOutput.Rows[i][outputName];
          }
          fxOutput.Rows.Add(itemArray);
        }

        fxOutput.AcceptChanges();

        var pathToOutputFile = Combine(pathToWorkingDirectory, $"{outputName.ToValidFileName()}.csv");
        SaveToCSV<object>(fxOutput, pathToOutputFile);
      }

      OutputNames.Iter(CompileOutput);

      cancellationToken.ThrowIfCancellationRequested();

      messages.OnNext((DateTime.Now, "Compiled per output"));
    }

    protected private static async Task<Arr<(string Parameter, double[] Sample)>>
    DoRankCorrelationAsync(
      Arr<(string Parameter, double[] Samples)> parameterSamples,
      Arr<(string Parameter, Arr<double> Correlations)> correlation,
      IAppService appService,
      CancellationToken cancellationToken
    )
    {
      ServerLicense? serverLicense = null;

      try
      {
        var maybeServerLicense = appService.RVisServerPool.RequestServer();
        serverLicense = maybeServerLicense.IfNone(() =>
        {
          appService.RVisServerPool.SlotFree.WaitOne(1000);
          return appService.RVisServerPool.RequestServer().IfNone(
            () => throw new Exception("No R server available")
          );
        });

        var rvisClient = await serverLicense.GetRClientAsync(cancellationToken);

        var samples = parameterSamples
          .Map(ps => ps.Samples)
          .ToArray()
          .Transpose();

        var objectNameSamples = "dvm_rc_ds";
        await rvisClient.CreateMatrixAsync(samples, objectNameSamples, cancellationToken);

        var correlations = correlation
          .Map(c => c.Correlations.ToArray())
          .ToArray();

        var objectNameCorrelations = "dvm_rc_corr";
        await rvisClient.CreateMatrixAsync(correlations, objectNameCorrelations, cancellationToken);

        var objectNameCorrelated = "dvm_rc_dsc";
        var code = $"{objectNameCorrelated} <- mc2d::cornode({objectNameSamples}, target={objectNameCorrelations})";

        await rvisClient.EvaluateNonQueryAsync(code, cancellationToken);

        var correlated = await rvisClient.EvaluateNumDataAsync(objectNameCorrelated, cancellationToken);

        parameterSamples = parameterSamples
          .Map((i, ps) => (ps.Parameter, correlated[i].Data.ToArray()))
          .ToArr();

        return parameterSamples;
      }
      finally
      {
        serverLicense?.Dispose();
      }
    }

    protected readonly IAppState _appState;
    protected readonly IAppService _appService;
  }
}
