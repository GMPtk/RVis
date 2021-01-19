using CsvHelper;
using LanguageExt;
using RVis.Base.Extensions;
using RVis.Data;
using RVis.Model;
using RVis.Model.Extensions;
using System;
using System.IO;
using static LanguageExt.Prelude;
using static RVis.Base.Check;
using static Sensitivity.Properties.Resources;
using static System.Globalization.CultureInfo;
using static System.IO.Path;
using static System.String;

namespace Sensitivity
{
  internal sealed partial class DesignViewModel
  {
    internal void Export(Arr<string> outputNames, string targetDirectory)
    {
      RequireNotNull(_moduleState.SensitivityDesign);

      string measureNames;
      string mainEffect;

      if (_moduleState.SensitivityDesign.SensitivityMethod == SensitivityMethod.Morris)
      {
        SensitivityDesign.ExportMorris(
          _moduleState.SensitivityDesign,
          _moduleState.MeasuresState.MorrisOutputMeasures,
          outputNames,
          targetDirectory
          );

        measureNames = "\"mu\", \"mustar\", \"sigma\"";
        mainEffect = "\"mustar\"";
      }
      else if (_moduleState.SensitivityDesign.SensitivityMethod == SensitivityMethod.Fast99)
      {
        SensitivityDesign.ExportFast99(
          _moduleState.SensitivityDesign,
          _moduleState.MeasuresState.Fast99OutputMeasures,
          outputNames,
          targetDirectory
          );

        measureNames = "\"firstorder\", \"totalorder\", \"variance\"";
        mainEffect = "\"firstorder\"";
      }
      else
      {
        throw new InvalidOperationException(nameof(SensitivityMethod));
      }

      var r = Format(
        InvariantCulture,
        FMT_LOAD_DATA,
        targetDirectory.Replace("\\", "/"),
        measureNames,
        mainEffect
        );

      File.WriteAllText(Combine(targetDirectory, "load_data.R"), r);

      var maybeOutputs = _sampleInputs.Map(i => _simData.GetOutput(i, _simulation));
      var allLoaded = maybeOutputs.ForAll(o => o.IsSome);

      if (allLoaded)
      {
        ExportOutputs(
          outputNames,
          targetDirectory,
          _simulation,
          maybeOutputs.Somes().ToArr()
          );
      }
    }

    private static void ExportOutputs(
      Arr<string> outputNames,
      string targetDirectory,
      Simulation simulation,
      Arr<NumDataTable> outputs
      )
    {
      var independentVariable = simulation.SimConfig.SimOutput.IndependentVariable;
      var output = outputs.Head();
      var independentData = simulation.SimConfig.SimOutput.GetIndependentData(output);

      void SaveOutputToCSV(string outputName)
      {
        var pathToCSV = Combine(targetDirectory, $"{outputName.ToValidFileName()}.csv");

        using var streamWriter = new StreamWriter(pathToCSV);
        using var csvWriter = new CsvWriter(streamWriter, InvariantCulture);

        csvWriter.WriteField(independentData.Name);

        Range(1, outputs.Count).Iter(i => csvWriter.WriteField($"Spl #{i}"));

        csvWriter.NextRecord();

        for (var i = 0; i < independentData.Length; ++i)
        {
          csvWriter.WriteField(independentData[i]);

          outputs.Iter(o =>
          {
            var dependentData = o[outputName];
            csvWriter.WriteField(dependentData[i]);
          });

          csvWriter.NextRecord();
        }
      }

      outputNames.Iter(SaveOutputToCSV);
    }
  }
}
