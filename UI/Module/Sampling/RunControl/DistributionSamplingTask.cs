using LanguageExt;
using Nett;
using RVis.Base.Extensions;
using RVis.Model;
using RVis.Model.Extensions;
using RVisUI.Model;
using System;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using static LanguageExt.Prelude;
using static RVis.Base.Check;
using static RVis.Data.FxData;
using static Sampling.Logger;
using static Sampling.Properties.Resources;
using static System.Double;
using static System.Globalization.CultureInfo;
using static System.IO.Path;
using static System.String;
using FxDataColumn = System.Data.DataColumn;
using FxDataTable = System.Data.DataTable;
using RvDataColumn = RVis.Data.NumDataColumn;

namespace Sampling
{
  internal sealed class DistributionSamplingTask : SamplingTaskBase
  {
    internal static IRunControlTask Create(TomlTable taskSpec, IAppState appState, IAppService appService)
    {
      RequireTrue(
        taskSpec.ContainsKey("n_samples"),
        "Distribution sampling task spec has missing n_samples setting"
        );
      RequireTrue(
        taskSpec.ContainsKey("distributions"),
        "Distribution sampling task spec has missing distributions"
        );
      RequireTrue(
        taskSpec.ContainsKey("outputs"),
        "Distribution sampling task spec has missing outputs"
        );

      var name = taskSpec["name"].Get<string>();
      RequireNotNullEmptyWhiteSpace(name, "Invalid task name");

      var nSamples = taskSpec["n_samples"].Get<int>();
      RequireTrue(nSamples > 0, "Invalid n_samples");

      var distributionSpecs = taskSpec["distributions"] as TomlArray;

      RequireNotNull(
        distributionSpecs,
        "Distribution sampling task spec has missing or misspecified distributions"
        );
      RequireTrue(
        distributionSpecs.Length > 0,
        "Distribution sampling task spec has empty distributions"
        );

      var distributions = distributionSpecs.Items
        .Select(tv => Distribution.ParseRelation(tv.Get<string>()))
        .ToArr();

      LatinHypercubeDesign latinHypercubeDesign = LatinHypercubeDesign.Default;

      if (taskSpec.ContainsKey("latin_hypercube"))
      {
        var latinHypercubeSpec = taskSpec["latin_hypercube"] as TomlTable;
        RequireNotNull(latinHypercubeSpec, "Expecting Latin hypercube specified as table array");
        RequireTrue(latinHypercubeSpec.ContainsKey("type"), "Latin hypercube missing type");

        var type = latinHypercubeSpec["type"].Get<LatinHypercubeDesignType>();
        var t0 = latinHypercubeSpec.ContainsKey("t0") ? latinHypercubeSpec["t0"].Get<double>() : NaN;
        var c = latinHypercubeSpec.ContainsKey("c") ? latinHypercubeSpec["c"].Get<double>() : NaN;
        var iterations = latinHypercubeSpec.ContainsKey("iter") ? latinHypercubeSpec["iter"].Get<int>() : default;
        var p = latinHypercubeSpec.ContainsKey("p") ? latinHypercubeSpec["p"].Get<double>() : NaN;
        var profile = latinHypercubeSpec.ContainsKey("profile") ? latinHypercubeSpec["profile"].Get<TemperatureDownProfile>() : default;
        var imax = latinHypercubeSpec.ContainsKey("imax") ? latinHypercubeSpec["imax"].Get<int>() : default;

        var haveDesignType = type != LatinHypercubeDesignType.None;

        var hasValidSA = IsNaN(t0) || (!IsNaN(c) && iterations > 0 && !IsNaN(p));

        RequireTrue(
          !haveDesignType || hasValidSA,
          "Invalid Latin hypercube configuration"
          );

        latinHypercubeDesign = new LatinHypercubeDesign(type, t0, c, iterations, p, profile, imax);
      }

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

        var correlationComplete = distributions.ForAll(
          d => correlation.Exists(c => c.Parameter == d.Variable)
          );
        RequireTrue(correlationComplete, "Incomplete correlation matrix");
        RequireTrue(correlation.Count == distributions.Count, "Unexpected data in correlation matrix");
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

      return new DistributionSamplingTask(
        name,
        nSamples,
        distributions,
        latinHypercubeDesign,
        correlation,
        outputNames,
        appState,
        appService
        );
    }

    public DistributionSamplingTask(
      string name,
      int nSamples,
      Arr<(string Parameter, IDistribution Distribution)> distributions,
      LatinHypercubeDesign latinHypercubeDesign,
      Arr<(string Parameter, Arr<double> Correlations)> correlation,
      Arr<string> outputNames,
      IAppState appState,
      IAppService appService
      ) : base(name, correlation, outputNames, appState, appService)
    {
      NSamples = nSamples;
      Distributions = distributions;
      LatinHypercubeDesign = latinHypercubeDesign;
    }

    public int NSamples { get; }
    public Arr<(string Parameter, IDistribution Distribution)> Distributions { get; }
    public LatinHypercubeDesign LatinHypercubeDesign { get; }

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

      var pathToOutputsDirectory = Combine(pathToWorkingDirectory, "outputs");
      if (Directory.Exists(pathToOutputsDirectory))
      {
        Directory.Delete(pathToOutputsDirectory, recursive: true);
      }

      cancellationToken.ThrowIfCancellationRequested();

      Arr<(string Parameter, IDistribution Distribution)> nonInvariants;
      Arr<(string Parameter, double[] Samples)> nonInvariantSamples;
      Arr<(string Parameter, IDistribution Distribution)> invariants;

      if (LatinHypercubeDesign.LatinHypercubeDesignType == LatinHypercubeDesignType.None)
      {
        (nonInvariants, nonInvariantSamples, invariants) = DoDistributionSampling(
          Name,
          NSamples,
          Distributions,
          simulation,
          messages
          );
      }
      else
      {
        (nonInvariants, nonInvariantSamples, invariants) = await DoInverseTransformSamplingAsync(
          Name,
          NSamples,
          Distributions,
          LatinHypercubeDesign,
          simulation,
          messages,
          _appService,
          cancellationToken
          );
      }

      if (!Correlation.IsEmpty)
      {
        nonInvariantSamples = Correlation
          .Map(c => nonInvariantSamples.Find(nis => nis.Parameter == c.Parameter)
          .AssertSome("Invariant samples / correlation mismatch"));

        nonInvariantSamples = await DoRankCorrelationAsync(
          nonInvariantSamples,
          Correlation,
          _appService,
          cancellationToken
          );

        messages.OnNext((DateTime.Now, "Applied rank correlation"));
      }

      var samplesTable = new FxDataTable();

      foreach (var nonInvariant in nonInvariants)
      {
        samplesTable.Columns.Add(new FxDataColumn(nonInvariant.Parameter, typeof(double)));
      }

      foreach (var invariant in invariants)
      {
        samplesTable.Columns.Add(new FxDataColumn(invariant.Parameter, typeof(double)));
      }

      var itemArray = new object[nonInvariants.Count + invariants.Count];

      for (var i = 0; i < NSamples; ++i)
      {
        nonInvariantSamples.Iter((j, nis) => itemArray[j] = nis.Samples[i]);
        invariants.Iter((j, inv) => itemArray[nonInvariantSamples.Count + j] = inv.Distribution.Mean);
        samplesTable.Rows.Add(itemArray);
      }

      samplesTable.AcceptChanges();

      SaveToCSV<double>(samplesTable, pathToSamplesFile);

      messages.OnNext((DateTime.Now, "Saved samples table"));

      return samplesTable;
    }

    private static
    (Arr<(string Parameter, IDistribution Distribution)> NonInvariants,
     Arr<(string Parameter, double[] Samples)> NonInvariantSamples,
     Arr<(string Parameter, IDistribution Distribution)> Invariants)
    DoDistributionSampling(
      string taskName,
      int nSamples,
      Arr<(string Parameter, IDistribution Distribution)> distributions,
      Simulation simulation,
      ISubject<(DateTime Timestamp, string Message)> messages
      )
    {
      var simInput = simulation.SimConfig.SimInput;

      (string Parameter, IDistribution Distribution)
        ToParamDistPair((string Parameter, IDistribution Distribution) distribution)
      {
        var parameter = simInput.SimParameters
          .FindParameter(distribution.Parameter)
          .AssertSome($"Non-existent parameter specified in task \"{taskName}\": {distribution.Parameter}");

        return (Parameter: parameter.Name, distribution.Distribution);
      }

      var nonInvariants = distributions
        .Filter(d => d.Distribution.DistributionType != DistributionType.Invariant)
        .Map(ToParamDistPair);

      var invariants = distributions
        .Filter(d => d.Distribution.DistributionType == DistributionType.Invariant)
        .Map(ToParamDistPair);

      var samples = new double[nSamples];

      var nonInvariantSamples = nonInvariants.Map(ni =>
      {
        ni.Distribution.FillSamples(samples);
        return (ni.Parameter, Samples: samples.ToArray());
      });

      messages.OnNext((DateTime.Now, "Generated samples table"));

      return (nonInvariants, nonInvariantSamples, invariants);
    }

    private static async Task<
      (Arr<(string Parameter, IDistribution Distribution)> NonInvariants,
       Arr<(string Parameter, double[] Samples)> NonInvariantSamples,
       Arr<(string Parameter, IDistribution Distribution)> Invariants)
      >
    DoInverseTransformSamplingAsync(
      string taskName,
      int nSamples,
      Arr<(string Parameter, IDistribution Distribution)> distributions,
      LatinHypercubeDesign latinHypercubeDesign,
      Simulation simulation,
      ISubject<(DateTime Timestamp, string Message)> messages,
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

        var simInput = simulation.SimConfig.SimInput;

        (string Parameter, IDistribution Distribution)
          ToParamDistPair((string Parameter, IDistribution Distribution) distribution)
        {
          var parameter = simInput.SimParameters
            .FindParameter(distribution.Parameter)
            .AssertSome($"Non-existent parameter specified in task \"{taskName}\": {distribution.Parameter}");

          return (Parameter: parameter.Name, distribution.Distribution);
        }

        var nonInvariants = distributions
          .Filter(d => d.Distribution.DistributionType != DistributionType.Invariant)
          .Map(ToParamDistPair);

        RequireFalse(nonInvariants.IsEmpty, "Latin hypercube requires one or more non-invariant distributions");

        var invariants = distributions
          .Filter(d => d.Distribution.DistributionType == DistributionType.Invariant)
          .Map(ToParamDistPair);

        var parameterBounds = nonInvariants
          .Map(ni =>
          {
            if (ni.Distribution is UniformDistribution uniformDistribution)
            {
              return (ni.Parameter, uniformDistribution.Lower, uniformDistribution.Upper, ICDF: fun<double, double>(d => d));
            }

            var (lower, upper) = ni.Distribution.IsTruncated
              ? ni.Distribution.CumulativeDistributionAtBounds
              : (0d, 1d);

            return (ni.Parameter, lower, upper, d => ni.Distribution.InverseCumulativeDistribution(d));
          });

        var n = nSamples;
        var dimension = parameterBounds.Count;
        var randomized =
          latinHypercubeDesign.LatinHypercubeDesignType == LatinHypercubeDesignType.Randomized
            ? "TRUE"
            : "FALSE";
        var seedValue = "NULL";

        var code = Format(
          InvariantCulture,
          FMT_LHSDESIGN,
          n,
          dimension,
          randomized,
          seedValue
          );

        var rvisClient = await serverLicense.GetRClientAsync(cancellationToken);

        await rvisClient.EvaluateNonQueryAsync(code, cancellationToken);

        var useSimulatedAnnealing = !IsNaN(latinHypercubeDesign.T0);

        RvDataColumn[] design;

        if (useSimulatedAnnealing)
        {
          var t0 = latinHypercubeDesign.T0;
          var c = latinHypercubeDesign.C;
          var it = latinHypercubeDesign.Iterations;
          var p = latinHypercubeDesign.P;
          var profile = latinHypercubeDesign.Profile switch
          {
            TemperatureDownProfile.Geometrical => "GEOM",
            TemperatureDownProfile.GeometricalMorris => "GEOM_MORRIS",
            TemperatureDownProfile.Linear => "LINEAR",
            _ => throw new InvalidOperationException($"Unsupported SA profile: {latinHypercubeDesign.Profile}")
          };
          var imax = latinHypercubeDesign.Imax;

          code = Format(
            InvariantCulture,
            FMT_MAXIMINSALHS,
            t0,
            c,
            it,
            p,
            profile,
            imax
            );

          await rvisClient.EvaluateNonQueryAsync(code, cancellationToken);
          design = await rvisClient.EvaluateNumDataAsync("rvis_lhsDesign_out_opt$design", cancellationToken);

          messages.OnNext((DateTime.Now, "Used simulated annealing"));
        }
        else
        {
          design = await rvisClient.EvaluateNumDataAsync("rvis_lhsDesign_out$design", cancellationToken);
        }

        var nonInvariantSamples = nonInvariants
          .Map((i, ni) =>
          {
            var (name, lower, upper, icdf) = parameterBounds[i];
            RequireTrue(name == ni.Parameter);

            var samples = design[i].Data
              .Select(d => icdf(lower + d * (upper - lower)))
              .ToArray();

            return (ni.Parameter, Samples: samples);
          })
          .ToArr();

        messages.OnNext((DateTime.Now, "Generated hypercube samples"));

        return (nonInvariants, nonInvariantSamples, invariants);
      }
      finally
      {
        serverLicense?.Dispose();
      }
    }
  }
}
