using LanguageExt;
using Nett;
using RVis.Model;
using RVisUI.Model;
using System;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using static RVis.Base.Check;
using static RVis.Data.FxData;
using static Sampling.Logger;
using static System.IO.Path;
using FxDataColumn = System.Data.DataColumn;
using FxDataRow = System.Data.DataRow;
using FxDataTable = System.Data.DataTable;

namespace Sampling
{
  internal sealed class FileSamplingTask : SamplingTaskBase
  {
    internal static IRunControlTask Create(TomlTable taskSpec, IAppState appState, IAppService appService)
    {
      RequireTrue(
        taskSpec.ContainsKey("samples_file"),
        "File sampling task spec has missing path to samples file"
        );
      RequireTrue(
        taskSpec.ContainsKey("outputs"),
        "File sampling task spec has missing outputs"
        );

      var name = taskSpec["name"].Get<string>();
      RequireNotNullEmptyWhiteSpace(name, "Invalid task name");

      var pathToSamplesFile = taskSpec["samples_file"].Get<string>();
      RequireFile(pathToSamplesFile, "Invalid samples_file");

      Arr<(string Parameter, Arr<double> Correlations)> correlation = default;

      if (taskSpec.ContainsKey("correlation"))
      {
        var correlationSpec = taskSpec["correlation"] as TomlTableArray;
        RequireNotNull(correlationSpec, "Expecting correlation specified as table array");
        correlation = correlationSpec.Items
          .Cast<TomlTable>()
          .Select(tt => (
            Parameter: tt["name"].Get<string>(),
            Correlations: tt["values"].Get<double[]>().ToArr()))
          .ToArr();
      }

      var outputSpecs = taskSpec["outputs"] as TomlArray;

      RequireNotNull(
        outputSpecs,
        "Distribution sampling task spec has missing or misspecified outputs"
        );
      RequireTrue(
        outputSpecs.Length > 0,
        "Distribution sampling task spec has empty outputs"
        );

      var outputNames = outputSpecs.To<string>().ToArr();

      return new FileSamplingTask(
        name,
        pathToSamplesFile,
        correlation,
        outputNames,
        appState,
        appService
        );
    }

    public FileSamplingTask(
      string name,
      string pathToSamplesFile,
      Arr<(string Parameter, Arr<double> Correlations)> correlation,
      Arr<string> outputNames,
      IAppState appState,
      IAppService appService
      ) : base(name, correlation, outputNames, appState, appService)
    {
      PathToSamplesFile = pathToSamplesFile;
    }

    public string PathToSamplesFile { get; }

    protected override async Task<FxDataTable> GetSamplesAsync(
      RunControlConfiguration configuration,
      Simulation simulation,
      string pathToWorkingDirectory,
      ISubject<(DateTime Timestamp, string Message)> messages,
      CancellationToken cancellationToken
      )
    {
      var pathToSamplesFile = Combine(pathToWorkingDirectory, "samples.csv");

      if (File.Exists(pathToSamplesFile))
      {
        try
        {
          var existingSamplesTable = LoadFromCSV<double>(pathToSamplesFile);
          messages.OnNext((DateTime.Now, "Loaded existing samples table"));
          return existingSamplesTable;
        }
        catch (Exception ex)
        {
          Log.Error(ex);
          File.Delete(pathToSamplesFile);
        }
      }

      FxDataTable? samplesTable = null;

      try
      {
        samplesTable = LoadFromCSV<double>(PathToSamplesFile);

        // (header checked in base)
        RequireTrue(samplesTable.Rows.Count > 0, $"Zero samples in {GetFileName(PathToSamplesFile)}");

        messages.OnNext((DateTime.Now, $"Loaded {samplesTable.Rows.Count} samples from {GetFileName(PathToSamplesFile)}"));
      }
      catch (Exception ex)
      {
        Log.Error(ex);
        messages.OnNext((DateTime.Now, $"Failed to load CSV from {GetFileName(PathToSamplesFile)}"));
        throw;
      }

      if (!Correlation.IsEmpty)
      {
        var correlationComplete = samplesTable.Columns.Cast<FxDataColumn>().All(
          dc => Correlation.Exists(c => c.Parameter == dc.ColumnName)
          );
        RequireTrue(correlationComplete, "Incomplete correlation matrix");
        RequireTrue(Correlation.Count == samplesTable.Columns.Count, "Unexpected data in correlation matrix");

        var parameterSamples = Correlation.Map(c => (
          Name: c.Parameter,
          Samples: samplesTable.Rows
            .Cast<FxDataRow>()
            .Select(dr => dr[c.Parameter])
            .Cast<double>()
            .ToArray()
          ));

        parameterSamples = await DoRankCorrelationAsync(
          parameterSamples,
          Correlation,
          _appService,
          cancellationToken
          );

        samplesTable = new FxDataTable();

        parameterSamples.Iter(
          ps => samplesTable.Columns.Add(new FxDataColumn(ps.Name, typeof(double)))
          );

        var nRows = parameterSamples.First().Samples.Length;
        var itemArray = new object[parameterSamples.Count];

        for (var i = 0; i < nRows; ++i)
        {
          parameterSamples.Iter((j, ps) => itemArray[j] = ps.Samples[i]);
          samplesTable.Rows.Add(itemArray);
        }

        samplesTable.AcceptChanges();

        messages.OnNext((DateTime.Now, "Applied rank correlation"));
      }

      SaveToCSV<double>(samplesTable, pathToSamplesFile);

      messages.OnNext((DateTime.Now, "Saved samples table"));

      return samplesTable;
    }
  }
}
