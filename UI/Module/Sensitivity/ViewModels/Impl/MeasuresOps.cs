using LanguageExt;
using RVis.Data;
using RVis.Data.Extensions;
using RVis.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using static LanguageExt.Prelude;
using static RVis.Base.Check;
using static Sensitivity.Properties.Resources;
using static System.Math;
using DataTable = System.Data.DataTable;

namespace Sensitivity
{
  internal static class MeasuresOps
  {
    internal static (DataTable FirstOrder, DataTable TotalOrder, DataTable Variance) GenerateOutputMeasures(
      string outputName,
      byte[] serializedDesign,
      DataTable samples,
      Arr<NumDataTable> designOutputs,
      IRVisClient client,
      CancellationToken cancellationToken,
      Action<string> progressHandler
      )
    {
      RequireEqual(samples.Rows.Count, designOutputs.Count);

      const string modelResponses = "rvis_sensitivity_out";
      const string sensitivityMeasures = "rvis_sensitivity_measures";

      client.LoadFromBinary(serializedDesign);

      cancellationToken.ThrowIfCancellationRequested();

      var nSamples = samples.Rows.Count;
      var nMeasures = designOutputs.Head().NRows;

      RequireTrue(designOutputs.ForAll(o => o.NRows == nMeasures));

      var measures = new List<Dictionary<string, double[]>>(nMeasures);

      var progressInterval = nMeasures / 50;
      progressInterval = Max(progressInterval, 1);

      for (var measure = 0; measure < nMeasures; ++measure)
      {
        var outputs = Range(0, nSamples).Map(
          sample => designOutputs[sample][outputName][measure]
          );
        client.CreateVector(outputs.ToArray(), modelResponses);
        client.EvaluateNonQuery(TELL_AND_COLLECT);
        measures.Add(client.EvaluateDoubles(sensitivityMeasures));

        if (measure % progressInterval == 0)
        {
          progressHandler($"Measures completed: {((1d + measure) / nMeasures):P0}");
        }

        cancellationToken.ThrowIfCancellationRequested();
      }

      var parameterFirstOrders = measures.Map(d => d["first"]).ToArr();
      var parameterTotalOrders = measures.Map(d => d["total"]).ToArr();
      var parameterVariances = measures.Map(d => d["V"]).ToArr();

      var factors = samples.Columns
        .Cast<DataColumn>()
        .Select(c => c.ColumnName)
        .ToArr();

      var independentVariable = designOutputs.Head().GetIndependentVariable();

      cancellationToken.ThrowIfCancellationRequested();

      var firstOrder = CreateDataTable(parameterFirstOrders, factors, independentVariable);
      var totalOrder = CreateDataTable(parameterTotalOrders, factors, independentVariable);
      var variance = CreateDataTable(parameterVariances, factors, independentVariable);

      return (firstOrder, totalOrder, variance);
    }

    private static DataTable CreateDataTable(
      Arr<double[]> parameterMeasures,
      Arr<string> factors,
      NumDataColumn independentVariable
      )
    {
      RequireTrue(parameterMeasures.Count == independentVariable.Length);
      RequireTrue(parameterMeasures.Head().Length == factors.Count);

      var dataTable = new DataTable();

      dataTable.Columns.Add(new DataColumn(independentVariable.Name, typeof(double)));

      foreach (var factor in factors)
      {
        dataTable.Columns.Add(new DataColumn(factor, typeof(double)));
      }

      var itemArray = new object[parameterMeasures.Head().Length + 1];

      parameterMeasures.Iter((i, pm) =>
      {
        itemArray[0] = independentVariable[i];
        System.Array.Copy(pm, 0, itemArray, 1, pm.Length);
        var dataRow = dataTable.NewRow();
        dataRow.ItemArray = itemArray;
        dataTable.Rows.Add(dataRow);
      });

      return dataTable;
    }
  }
}
