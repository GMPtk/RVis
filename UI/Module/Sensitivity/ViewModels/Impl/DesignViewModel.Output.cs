using LanguageExt;
using RVis.Base.Extensions;
using RVis.Model;
using RVis.Model.Extensions;
using RVisUI.Model;
using System.Data;
using System.Linq;
using System.Reactive.Linq;
using static RVis.Base.Check;
using DataTable = System.Data.DataTable;

namespace Sensitivity
{
  internal sealed partial class DesignViewModel
  {
    private static (SimInput Input, bool OutputRequested, Arr<double> Output)[] CompileOutputRequestJob(
      string outputName,
      Simulation simulation,
      ISimData simData,
      Arr<DataTable> samples,
      Arr<DesignParameter> invariants
      )
    {
      RequireTrue(invariants.ForAll(
        dp => dp.Distribution.DistributionType == DistributionType.Invariant
        ));

      var totalNoOfRows = samples.Sum(dt => dt.Rows.Count);
      RequireTrue(totalNoOfRows > 0);

      var job = new (SimInput Input, bool OutputRequested, Arr<double> Output)[totalNoOfRows];
      var defaultInput = simulation.SimConfig.SimInput;

      var targetParameters = samples.Head().Columns
        .Cast<DataColumn>()
        .Select(dc => defaultInput.SimParameters.GetParameter(dc.ColumnName))
        .ToArr();

      var invariantParameters = invariants.Map(dp =>
      {
        var parameter = defaultInput.SimParameters.GetParameter(dp.Name);
        return parameter.With(dp.Distribution.Mean);
      });

      var jobItem = 0;

      samples.Iter(dt =>
      {
        for (var row = 0; row < dt.Rows.Count; ++row)
        {
          var dataRow = dt.Rows[row];

          var sampleParameters = targetParameters
            .Map((i, p) => p.With(dataRow.Field<double>(i)))
            .ToArr();

          var input = defaultInput.With(sampleParameters + invariantParameters);

          var output = simData.GetOutput(input, simulation).Match(
            o => o[outputName].Data.ToArr(),
            () => default
            );

          job[jobItem] = (input, false, output);
          ++jobItem;
        }
      });

      return job;
    }

    private void Measure(Arr<Arr<double>> designOutputs)
    {
      var outputName = _moduleState.MeasuresState.SelectedOutputName;
      RequireTrue(outputName.IsAString());

      if (_moduleState.SensitivityDesign.SensitivityMethod == SensitivityMethod.Morris)
      {
        if (_moduleState.MeasuresState.MorrisOutputMeasures.ContainsKey(outputName)) return;

        var loadedMeasures = _sensitivityDesigns.LoadMorrisOutputMeasures(
          _moduleState.SensitivityDesign,
          outputName,
          out (DataTable Mu, DataTable MuStar, DataTable Sigma) measures
          );

        if (loadedMeasures)
        {
          _moduleState.MeasuresState.MorrisOutputMeasures =
            _moduleState.MeasuresState.MorrisOutputMeasures.Add(outputName, measures);
          return;
        }
      }
      else
      {
        if (_moduleState.MeasuresState.Fast99OutputMeasures.ContainsKey(outputName)) return;

        var loadedMeasures = _sensitivityDesigns.LoadFast99OutputMeasures(
          _moduleState.SensitivityDesign,
          outputName,
          out (DataTable FirstOrder, DataTable TotalOrder, DataTable Variance) measures
          );

        if (loadedMeasures)
        {
          _moduleState.MeasuresState.Fast99OutputMeasures =
            _moduleState.MeasuresState.Fast99OutputMeasures.Add(outputName, measures);
          return;
        }
      }

      void SomeServer(ServerLicense serverLicense)
      {
        var _ = _moduleState.SensitivityDesign.SensitivityMethod == SensitivityMethod.Morris
          ? GenerateMorrisOutputMeasuresAsync(outputName, designOutputs, serverLicense)
          : GenerateFast99OutputMeasuresAsync(outputName, designOutputs, serverLicense);
      }

      void NoServer()
      {
        _appService.Notify(
          NotificationType.Information,
          nameof(DesignViewModel),
          nameof(Measure),
          "No R server available."
          );
      }

      _appService.RVisServerPool.RequestServer().Match(SomeServer, NoServer);
    }
  }
}
