using LanguageExt;
using RVis.Base.Extensions;
using RVis.Data;
using RVis.Data.Extensions;
using RVis.Model;
using RVis.Model.Extensions;
using RVisUI.AppInf;
using RVisUI.AppInf.Extensions;
using System;
using System.Data;
using System.Linq;
using System.Reactive.Linq;
using static RVis.Base.Check;
using static RVis.Base.Extensions.NumExt;
using static Sensitivity.DesignParameter;
using static Sensitivity.Properties.Resources;
using static System.Double;
using static System.Globalization.CultureInfo;
using static System.String;
using DataTable = System.Data.DataTable;

namespace Sensitivity
{
  internal sealed partial class DesignViewModel
  {
    private static Arr<NumDataTable> ToCommonIndependent(Arr<NumDataTable> designOutputs)
    {
      var head = designOutputs.Head();

      var commonIndependentData = new System.Collections.Generic.HashSet<double>(
        head.GetIndependentVariable().Data
        );

      designOutputs.Tail().Iter(
        ndt => commonIndependentData.IntersectWith(ndt.GetIndependentVariable().Data)
        );

      var targetColumnData = head.NumDataColumns
        .Select(_ => new double[commonIndependentData.Count])
        .ToList();
      var columnNames = head.ColumnNames;

      return designOutputs.Map(ndt =>
      {
        if (ndt.NRows == commonIndependentData.Count) return ndt;

        var independentVar = ndt.GetIndependentVariable();
        var sourceRowIndices = commonIndependentData
          .Select(x => independentVar.Data.FindIndex(v => v == x))
          .ToArray();

        RequireFalse(sourceRowIndices.Any(r => r == NOT_FOUND));

        for (var targetRowIndex = 0; targetRowIndex < sourceRowIndices.Length; ++targetRowIndex)
        {
          var sourceRowIndex = sourceRowIndices[targetRowIndex];
          ndt.NumDataColumns.Iter(
            (column, ndc) => targetColumnData[column][targetRowIndex] = ndc[sourceRowIndex]
            );
        }

        var numDataColumns = columnNames.Select(
          (cn, column) => new NumDataColumn(cn, targetColumnData[column])
          );

        return new NumDataTable(ndt.Name, numDataColumns);
      });
    }

    private static (DataTable Samples, byte[] SerializedDesign) GetFast99Samples(
      Arr<ParameterState> parameterStates,
      int sampleSize,
      IRVisClient rVisClient
      )
    {
      var parameterDistributions = parameterStates
        .Filter(ps => ps.IsSelected)
        .OrderBy(ps => ps.Name.ToUpperInvariant())
        .Select(ps => (ps.Name, Distribution: ps.GetDistribution()))
        .ToArr();

      var nSelected = parameterDistributions.Count;

      RequireFalse(nSelected == 0, "Configure one or more parameter distributions.");

      parameterDistributions = parameterDistributions.Filter(pd => pd.Distribution.IsConfigured);

      var nConfigured = parameterDistributions.Count;

      RequireFalse(nConfigured < nSelected, "One or more parameter distributions not correctly configured.");

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
        throw new InvalidOperationException("Parameter choices inconsistent with lower and upper bounds supplied: " + names);
      }

      var signatures =
        samplingParameterDistributions.Map(spd => spd.RequiresInverseTransformSampling
          ? spd.Distribution.RInverseTransformSamplingSignature
          : spd.Distribution.RQuantileSignature
        );

      var factors = Join(", ", samplingParameterDistributions.Map(spd => $"\"{spd.Name}\""));
      var omegaFromCukier = false.ToString(InvariantCulture).ToUpperInvariant();
      var qfs = Join(", ", signatures.Map(s => $"\"{s.FunctionName}\""));

      string FuncParamsToListArgs(Arr<(string ArgName, double ArgValue)> funcParams) =>
        Join(", ", funcParams.Map(fp => $"{fp.ArgName} = {fp.ArgValue.ToString(InvariantCulture)}"));

      var qfargs = signatures.Count == 1
        ? FuncParamsToListArgs(signatures.Head().FunctionParameters)
        : Join(", ", signatures.Map(s => $"list({FuncParamsToListArgs(s.FunctionParameters)})"));

      var code = Format(
        InvariantCulture,
        FMT_CREATE_DESIGN,
        sampleSize,
        factors,
        omegaFromCukier,
        qfs,
        qfargs
        );

      rVisClient.EvaluateNonQuery(code);

      var X = rVisClient.EvaluateDoubles("rvis_sensitivity_design[[\"X\"]]");

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

      var serializedDesign = rVisClient.SaveObjectToBinary("rvis_sensitivity_design");

      return (samples, serializedDesign);
    }

    private static bool UpdateInputsImpl(DataTable inputs, (SimInput Input, bool OutputRequested, NumDataTable Output)[] outputRequestJob)
    {
      RequireEqual(inputs.Rows.Count, outputRequestJob.Length);

      var acquiredColumn = inputs.Columns[ACQUIRED_DATACOLUMN_NAME];
      var foundIssues = false;

      for (var i = 0; i < outputRequestJob.Length; ++i)
      {
        var (input, _, output) = outputRequestJob[i];
        var dataRow = inputs.Rows[i];

        var acquired = input == default
          ? AcquiredType.Error
          : output == default
            ? IsSuspect(dataRow)
              ? AcquiredType.Suspect
              : AcquiredType.No
            : IsSuspect(output)
              ? AcquiredType.Suspect
              : AcquiredType.Yes;

        if (acquired == AcquiredType.Suspect || acquired == AcquiredType.Error)
        {
          foundIssues = true;
        }

        dataRow[acquiredColumn] = acquired.ToString();
      }

      return foundIssues;
    }

    private static bool IsSuspect(DataRow dataRow) =>
      dataRow.ItemArray.OfType<double>().Any(d => IsInfinity(d) || IsNaN(d));

    private static bool IsSuspect(NumDataTable output) =>
      output.NumDataColumns.Any(ndc => ndc.Data.Any(IsNaN));

    private static (SimInput Input, bool OutputRequested, NumDataTable Output)[] CompileOutputRequestJob(
      Simulation simulation,
      ISimData simData,
      DataTable samples,
      Arr<DesignParameter> invariants
      )
    {
      var job = new (SimInput Input, bool OutputRequested, NumDataTable Output)[samples.Rows.Count];
      var defaultInput = simulation.SimConfig.SimInput;

      var targetParameters = samples.Columns
        .Cast<DataColumn>()
        .Select(dc => defaultInput.SimParameters.GetParameter(dc.ColumnName))
        .ToArr();

      var invariantParameters = invariants.Map(dp =>
      {
        var parameter = defaultInput.SimParameters.GetParameter(dp.Name);
        return parameter.With(dp.Distribution.Mean);
      });

      for (var row = 0; row < samples.Rows.Count; ++row)
      {
        var dataRow = samples.Rows[row];

        var sampleParameters = targetParameters
          .Map((i, p) => p.With(dataRow.Field<double>(i)))
          .ToArr();

        var input = defaultInput.With(sampleParameters + invariantParameters);

        var output = simData.GetOutput(input, simulation).IfNoneUnsafe(default(NumDataTable));

        job[row] = (input, false, output);
      }

      return job;
    }

    private static DataTable CreateInputs(DataTable samples)
    {
      var inputs = samples.Copy();
      var acquiredColumn = new DataColumn(ACQUIRED_DATACOLUMN_NAME, typeof(string));
      inputs.Columns.Add(acquiredColumn);
      return inputs;
    }
  }
}
