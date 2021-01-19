using LanguageExt;
using RVis.Data;
using RVis.Model;
using RVis.Model.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static LanguageExt.Prelude;
using static RVis.Base.Check;
using static Sensitivity.Properties.Resources;
using static System.Environment;
using static System.Globalization.CultureInfo;
using static System.Math;
using static System.String;
using DataColumn = System.Data.DataColumn;
using DataTable = System.Data.DataTable;

namespace Sensitivity
{
  internal static class Method
  {
    internal static async Task<(Arr<DataTable> Samples, Arr<byte[]> SerializedDesigns)> GetMorrisSamplesAsync(
      Arr<(string Name, IDistribution Distribution)> parameterDistributions,
      int noOfRuns,
      IRVisClient rVisClient
      )
    {
      var samplingParameterDistributions = parameterDistributions
        .Filter(
          pd => pd.Distribution.DistributionType != DistributionType.Invariant
        )
        .Map(pd =>
        {
          double lower;
          double upper;

          if (pd.Distribution.DistributionType == DistributionType.Uniform)
          {
            var uniform = RequireInstanceOf<UniformDistribution>(pd.Distribution);
            lower = uniform.Lower;
            upper = uniform.Upper;
          }
          else
          {
            if (pd.Distribution.IsTruncated)
            {
              (lower, upper) = pd.Distribution.CumulativeDistributionAtBounds;
            }
            else
            {
              lower = 0d;
              upper = 1d;
            }
          }

          return (
            pd.Name,
            pd.Distribution,
            binf: lower,
            bsup: upper
          );
        });

      var serializedDesigns = new List<byte[]>(noOfRuns);
      var samples = new List<DataTable>(noOfRuns);

      var factors = Join(", ", samplingParameterDistributions.Map(spd => $"\"{spd.Name}\""));
      var r = 4;
      var binf = Join(", ", samplingParameterDistributions.Map(spd => spd.binf));
      var bsup = Join(", ", samplingParameterDistributions.Map(spd => spd.bsup));
      var levels = 5;
      var gridJump = 3;

      for (var i = 0; i < noOfRuns; ++i)
      {
        var code = Format(
          InvariantCulture,
          FMT_CREATE_MORRIS_DESIGN,
          factors,
          r,
          binf,
          bsup,
          levels,
          gridJump,
          i + 1
          );

        await rVisClient.EvaluateNonQueryAsync(code);

        var X = await rVisClient.EvaluateDoublesAsync(
          $"rvis_sensitivity_design_{(i + 1):00000000}[[\"X\"]]"
          );

        var dataTable = new DataTable();

        samplingParameterDistributions.Iter(
          spd => dataTable.Columns.Add(new DataColumn(spd.Name, typeof(double)))
          );

        var nRows = X.Values.First().Length;

        for (var row = 0; row < nRows; ++row)
        {
          var values = samplingParameterDistributions
            .Map(spd =>
            {
              var v = X[spd.Name][row];

              if (spd.Distribution.DistributionType != DistributionType.Uniform)
              {
                v = spd.Distribution.InverseCumulativeDistribution(v);
              }

              return v;
            })
            .Cast<object>()
            .ToArray();

          dataTable.Rows.Add(values);
        }

        var serializedDesign = await rVisClient.SaveObjectToBinaryAsync(
          $"rvis_sensitivity_design_{(i + 1):00000000}"
          );

        serializedDesigns.Add(serializedDesign);

        dataTable.AcceptChanges();
        samples.Add(dataTable);
      }

      return (samples.ToArr(), serializedDesigns.ToArr());
    }

    internal static async Task<(DataTable Mu, DataTable MuStar, DataTable Sigma)> GenerateMorrisOutputMeasuresAsync(
      Arr<byte[]> serializedDesigns,
      Arr<DataTable> samples,
      Arr<Arr<double>> designOutputs,
      NumDataColumn independentVariable,
      IRVisClient client,
      Action<string> progressHandler,
      CancellationToken cancellationToken
      )
    {
      RequireFalse(serializedDesigns.IsEmpty);
      RequireFalse(samples.IsEmpty);
      RequireFalse(designOutputs.IsEmpty);

      var noOfRuns = serializedDesigns.Count;
      RequireEqual(noOfRuns, samples.Count);

      var totalNoOfSamples = samples.Sum(dt => dt.Rows.Count);
      RequireEqual(totalNoOfSamples, designOutputs.Count);

      var nMeasures = independentVariable.Length;
      RequireTrue(designOutputs.ForAll(a => a.Count == nMeasures));

      serializedDesigns.Iter(async b => await client.LoadFromBinaryAsync(b, cancellationToken));

      cancellationToken.ThrowIfCancellationRequested();

      var factors = samples.Head().Columns
        .Cast<DataColumn>()
        .Select(dc => dc.ColumnName)
        .ToArr();

      var mus = new SortedDictionary<string, double[]>();
      var muStars = new SortedDictionary<string, double[]>();
      var sigmas = new SortedDictionary<string, double[]>();

      factors.Iter(f =>
      {
        mus.Add(f, new double[nMeasures]);
        muStars.Add(f, new double[nMeasures]);
        sigmas.Add(f, new double[nMeasures]);
      });

      var progressInterval = nMeasures / 50;
      progressInterval = Max(progressInterval, 1);

      for (var measure = 0; measure < nMeasures; ++measure)
      {
        var designOutputsTaken = 0;

        var tellAndCollects = Range(0, noOfRuns).Map(i =>
        {
          var nOutputs = samples[i].Rows.Count;
          var outputs = designOutputs
            .Skip(designOutputsTaken)
            .Take(nOutputs)
            .Select(a => a[measure])
            .ToArr();
          designOutputsTaken += nOutputs;

          var tellAndCollect = Format(
            InvariantCulture,
            FMT_MORRIS_TELL_AND_COLLECT,
            i + 1,
            Join(", ", outputs)
            );

          return tellAndCollect;
        });

        var code = Join(NewLine, tellAndCollects);

        await client.EvaluateNonQueryAsync(code, cancellationToken);

        for (var run = 0; run < noOfRuns; ++run)
        {
          var results = await client.EvaluateDoublesAsync($"rvis_p_{(run + 1):00000000}", cancellationToken);

          factors.Iter((i, f) =>
          {
            mus[f][measure] += results["mu"][i];
            muStars[f][measure] += results["mu.star"][i];
            sigmas[f][measure] += results["sigma"][i];
          });
        }

        if (measure % progressInterval == 0)
        {
          progressHandler($"Measures completed: {((1d + measure) / nMeasures):P0}");
        }

        cancellationToken.ThrowIfCancellationRequested();
      }

      var parameterMeasures = Range(0, nMeasures).Map(m => factors.Map(f => mus[f][m] / noOfRuns)).ToArr();
      var mu = CreateDataTable(parameterMeasures, factors, independentVariable);

      parameterMeasures = Range(0, nMeasures).Map(m => factors.Map(f => muStars[f][m] / noOfRuns)).ToArr();
      var muStar = CreateDataTable(parameterMeasures, factors, independentVariable);

      parameterMeasures = Range(0, nMeasures).Map(m => factors.Map(f => sigmas[f][m] / noOfRuns)).ToArr();
      var sigma = CreateDataTable(parameterMeasures, factors, independentVariable);

      return (mu, muStar, sigma);
    }

    internal static async Task<(DataTable Samples, byte[] SerializedDesign)> GetFast99SamplesAsync(
      Arr<(string Name, IDistribution Distribution)> parameterDistributions,
      int noOfSamples,
      IRVisClient rVisClient
      )
    {
      var samplingParameterDistributions = parameterDistributions
        .Filter(
          pd => pd.Distribution.DistributionType != DistributionType.Invariant
        )
        .Map(pd =>
          (
            pd.Name,
            pd.Distribution,
            RequiresInverseTransformSampling: pd.Distribution.RequiresInverseTransformSampling(),
            pd.Distribution.CumulativeDistributionAtBounds
          )
        );

      var itsCDAtBounds = samplingParameterDistributions
        .Filter(spd =>
          spd.RequiresInverseTransformSampling &&
          (spd.CumulativeDistributionAtBounds.LowerP == 0d || spd.CumulativeDistributionAtBounds.UpperP == 1d)
          );

      if (!itsCDAtBounds.IsEmpty)
      {
        var names = Join(", ", itsCDAtBounds.Map(spd => spd.Name));
        throw new InvalidOperationException(
          "Parameter choices inconsistent with lower and upper bounds supplied: " + names
          );
      }

      var signatures =
        samplingParameterDistributions.Map(spd => spd.RequiresInverseTransformSampling
          ? spd.Distribution.RInverseTransformSamplingSignature
          : spd.Distribution.RQuantileSignature
        );

      var factors = Join(", ", samplingParameterDistributions.Map(spd => $"\"{spd.Name}\""));
      var omegaFromCukier = false.ToString(InvariantCulture).ToUpperInvariant();
      var qfs = Join(", ", signatures.Map(s => $"\"{s.FunctionName}\""));

      static string FuncParamsToListArgs(Arr<(string ArgName, double ArgValue)> funcParams) =>
        Join(", ", funcParams.Map(fp => $"{fp.ArgName} = {fp.ArgValue.ToString(InvariantCulture)}"));

      var qfargs = signatures.Count == 1
        ? FuncParamsToListArgs(signatures.Head().FunctionParameters)
        : Join(", ", signatures.Map(s => $"list({FuncParamsToListArgs(s.FunctionParameters)})"));

      var code = Format(
        InvariantCulture,
        FMT_CREATE_FAST99_DESIGN,
        noOfSamples,
        factors,
        omegaFromCukier,
        qfs,
        qfargs
        );

      await rVisClient.EvaluateNonQueryAsync(code);

      var X = await rVisClient.EvaluateDoublesAsync("rvis_sensitivity_design[[\"X\"]]");

      var samples = new DataTable();

      samplingParameterDistributions.Iter(
        spd => samples.Columns.Add(new DataColumn(spd.Name, typeof(double)))
        );

      var nRows = X.Values.First().Length;

      for (var row = 0; row < nRows; ++row)
      {
        var values = samplingParameterDistributions
          .Map(spd =>
          {
            var v = X[spd.Name][row];

            if (spd.RequiresInverseTransformSampling)
            {
              v = spd.Distribution.InverseCumulativeDistribution(v);
            }

            return v;
          })
          .Cast<object>()
          .ToArray();

        samples.Rows.Add(values);
      }

      samples.AcceptChanges();

      var serializedDesign = await rVisClient.SaveObjectToBinaryAsync("rvis_sensitivity_design");

      return (samples, serializedDesign);
    }

    internal static async Task<(DataTable FirstOrder, DataTable TotalOrder, DataTable Variance)> GenerateFast99OutputMeasuresAsync(
      byte[] serializedDesign,
      DataTable samples,
      Arr<Arr<double>> designOutputs,
      NumDataColumn independentVariable,
      IRVisClient client,
      Action<string> progressHandler,
      CancellationToken cancellationToken
      )
    {
      RequireEqual(samples.Rows.Count, designOutputs.Count);

      const string modelResponses = "rvis_sensitivity_out";
      const string sensitivityMeasures = "rvis_sensitivity_measures";

      await client.LoadFromBinaryAsync(serializedDesign, cancellationToken);

      cancellationToken.ThrowIfCancellationRequested();

      var nSamples = samples.Rows.Count;
      var nMeasures = designOutputs.Head().Count;

      RequireTrue(designOutputs.ForAll(o => o.Count == nMeasures));

      var measures = new List<Dictionary<string, double[]>>(nMeasures);

      var progressInterval = nMeasures / 50;
      progressInterval = Max(progressInterval, 1);

      for (var measure = 0; measure < nMeasures; ++measure)
      {
        var outputs = Range(0, nSamples).Map(
          sample => designOutputs[sample][measure]
          );
        await client.CreateVectorAsync(outputs.ToArray(), modelResponses, cancellationToken);
        await client.EvaluateNonQueryAsync(FAST99_TELL_AND_COLLECT, cancellationToken);
        measures.Add(await client.EvaluateDoublesAsync(sensitivityMeasures, cancellationToken));

        if (measure % progressInterval == 0)
        {
          progressHandler($"Measures completed: {((1d + measure) / nMeasures):P0}");
        }

        cancellationToken.ThrowIfCancellationRequested();
      }

      var parameterFirstOrders = measures.Map(d => d["first"].ToArr()).ToArr();
      var parameterTotalOrders = measures.Map(d => d["total"].ToArr()).ToArr();
      var parameterVariances = measures.Map(d => d["V"].ToArr()).ToArr();

      var factors = samples.Columns
        .Cast<DataColumn>()
        .Select(c => c.ColumnName)
        .ToArr();

      cancellationToken.ThrowIfCancellationRequested();

      var firstOrder = CreateDataTable(parameterFirstOrders, factors, independentVariable);
      var totalOrder = CreateDataTable(parameterTotalOrders, factors, independentVariable);
      var variance = CreateDataTable(parameterVariances, factors, independentVariable);

      return (firstOrder, totalOrder, variance);
    }

    private static DataTable CreateDataTable(
      Arr<Arr<double>> parameterMeasures,
      Arr<string> factors,
      NumDataColumn independentVariable
      )
    {
      RequireTrue(parameterMeasures.Count == independentVariable.Length);
      RequireTrue(parameterMeasures.Head().Count == factors.Count);

      var dataTable = new DataTable();

      dataTable.Columns.Add(new DataColumn(independentVariable.Name, typeof(double)));

      foreach (var factor in factors)
      {
        dataTable.Columns.Add(new DataColumn(factor, typeof(double)));
      }

      var itemArray = new object[parameterMeasures.Head().Count + 1];

      parameterMeasures.Iter((i, pm) =>
      {
        itemArray[0] = independentVariable[i];
        pm.Iter((j, d) => itemArray[j + 1] = d);
        var dataRow = dataTable.NewRow();
        dataRow.ItemArray = itemArray;
        dataTable.Rows.Add(dataRow);
      });

      dataTable.AcceptChanges();

      return dataTable;
    }
  }
}
