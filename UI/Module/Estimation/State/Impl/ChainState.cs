using LanguageExt;
using Nett;
using RVis.Base.Extensions;
using RVis.Model;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using static LanguageExt.Prelude;
using static RVis.Base.Check;
using static RVis.Data.FxData;
using static System.Double;
using static System.Globalization.CultureInfo;
using static System.IO.Path;

namespace Estimation
{
  internal partial class ChainState
  {
    internal static void Save(Arr<ChainState> chainStates, string pathToEstimationDesign)
    {
      RequireDirectory(pathToEstimationDesign);

      var pathToChainState = Combine(pathToEstimationDesign, nameof(ChainState).ToLowerInvariant());

      Directory.CreateDirectory(pathToChainState);

      chainStates.Iter(cs => SaveChainState(cs, pathToChainState));
    }

    internal static Arr<ChainState> Load(string pathToEstimationDesign)
    {
      RequireDirectory(pathToEstimationDesign);

      var pathToChainState = Combine(pathToEstimationDesign, nameof(ChainState).ToLowerInvariant());

      if (!Directory.Exists(pathToChainState)) return default;

      var chainStateDirectories = new DirectoryInfo(pathToChainState).GetDirectories();

      var chainStates = chainStateDirectories.Select(LoadChainState);

      return chainStates.Somes().ToArr();
    }

    internal static void ExportChainState(
      string targetDirectory,
      Arr<string> targetOutputs,
      Simulation simulation,
      ChainState chainState
      )
    {
      RequireNotNull(chainState.ChainData);
      RequireNotNull(chainState.ErrorData);
      RequireNotNull(chainState.PosteriorData);

      var pathToChainState = Combine(targetDirectory, chainState.No.ToString(InvariantCulture));
      Directory.CreateDirectory(pathToChainState);

      using var chainData = chainState.ChainData.Copy();
      var rowsToRemove = chainData.Rows
        .Cast<DataRow>()
        .Where(dr => IsNaN(dr.Field<double>(0)))
        .ToArr();
      rowsToRemove.Iter(chainData.Rows.Remove);
      chainData.AcceptChanges();
      SaveChainData(chainData, pathToChainState);

      using var errorData = chainState.ErrorData.Copy();
      rowsToRemove = errorData.Rows
        .Cast<DataRow>()
        .Where(dr => IsNaN(dr.Field<double>(0)))
        .ToArr();
      rowsToRemove.Iter(errorData.Rows.Remove);
      errorData.AcceptChanges();
      SaveErrorData(errorData, pathToChainState);

      var independentName = simulation.SimConfig.SimOutput.IndependentVariable.Name;

      targetOutputs.Iter(o =>
      {
        var posteriorData = chainState.PosteriorData[o];

        var lastRow = 0;
        for (; lastRow < posteriorData.Rows.Count; ++lastRow)
        {
          if (IsNaN(posteriorData.Rows[lastRow].Field<double>(0))) break;
        }

        if (lastRow < 2) return;

        var independentData = posteriorData.Rows[0].ItemArray
        .Cast<double>()
        .ToArray();

        using var dataTable = new DataTable();
        dataTable.Columns.Add(new DataColumn(independentName, typeof(double)));
        Range(1, lastRow - 1).Iter(
          i => dataTable.Columns.Add(new DataColumn($"it #{i:00000000}", typeof(double)))
          );

        for (var i = 0; i < posteriorData.Columns.Count; ++i)
        {
          var destinationRow = dataTable.NewRow();
          destinationRow[0] = independentData[i];

          for (var j = 1; j < lastRow; ++j)
          {
            destinationRow[j] = posteriorData.Rows[j][i];
          }

          dataTable.Rows.Add(destinationRow);
        }

        dataTable.AcceptChanges();

        var pathToPosteriorData = Combine(pathToChainState, $"{o}.csv");
        SaveToCSV<double>(dataTable, pathToPosteriorData);
      });
    }

    private static void SaveChainState(ChainState chainState, string pathToChainState)
    {
      RequireNotNull(chainState.ChainData);
      RequireNotNull(chainState.ErrorData);
      RequireNotNull(chainState.PosteriorData);

      pathToChainState = Combine(pathToChainState, chainState.No.ToString(InvariantCulture));

      SaveModelParameters(chainState.ModelParameters, pathToChainState);
      SaveModelOutputs(chainState.ModelOutputs, pathToChainState);
      SaveChainData(chainState.ChainData, pathToChainState);
      SaveErrorData(chainState.ErrorData, pathToChainState);
      SavePosteriorData(chainState.PosteriorData, pathToChainState);
    }

    private static Option<ChainState> LoadChainState(DirectoryInfo chainStateDirectory)
    {
      if (!int.TryParse(chainStateDirectory.Name, out int no)) return None;

      var modelParameters = LoadModelParameters(chainStateDirectory);
      var modelOutputs = LoadModelOutputs(chainStateDirectory);
      var chainData = LoadChainData(chainStateDirectory);
      var errorData = LoadErrorData(chainStateDirectory);
      var posteriorData = LoadPosteriorData(chainStateDirectory, modelOutputs.Map(mo => mo.Name));

      return new ChainState(no, modelParameters, modelOutputs, chainData, errorData, posteriorData);
    }

    private class _ModelParameterDTO
    {
      public string? Name { get; set; }
      public string? Distribution { get; set; }
      public double? Value { get; set; }
      public double? Step { get; set; }
    }

    private class _ModelParametersDTO
    {
      public _ModelParameterDTO[]? ModelParameters { get; set; }
    }

    private class _ModelOutputDTO
    {
      public string? Name { get; set; }
      public string? ErrorModel { get; set; }
    }

    private class _ModelOutputsDTO
    {
      public _ModelOutputDTO[]? ModelOutputs { get; set; }
    }

    private static void SaveModelParameters(Arr<ModelParameter> modelParameters, string pathToChainState)
    {
      var pathToModelParameters = Combine(pathToChainState, $"{nameof(ModelParameters).ToLowerInvariant()}.toml");
      var dto = new _ModelParametersDTO
      {
        ModelParameters = modelParameters
          .Map(mp => new _ModelParameterDTO
          {
            Name = mp.Name,
            Distribution = mp.Distribution.ToString(),
            Value = mp.Value.ToNullable(),
            Step = mp.Step.ToNullable()
          })
          .ToArray()
      };
      Toml.WriteFile(dto, pathToModelParameters);
    }

    private static Arr<ModelParameter> LoadModelParameters(DirectoryInfo chainStateDirectory)
    {
      var pathToModelParameters = Combine(chainStateDirectory.FullName, $"{nameof(ModelParameters).ToLowerInvariant()}.toml");
      if (!File.Exists(pathToModelParameters)) return default;

      var modelParameters = Toml.ReadFile<_ModelParametersDTO>(pathToModelParameters);

      RequireNotNull(modelParameters.ModelParameters);

      return modelParameters.ModelParameters
        .Select(dto => new ModelParameter(
          dto.Name.AssertNotNull(),
          Distribution.DeserializeDistribution(dto.Distribution).AssertSome(),
          dto.Value.FromNullable(),
          dto.Step.FromNullable()
          ))
        .ToArr();
    }

    private static void SaveModelOutputs(Arr<ModelOutput> modelOutputs, string pathToChainState)
    {
      var pathToModelOutputs = Combine(pathToChainState, $"{nameof(ModelOutputs).ToLowerInvariant()}.toml");
      var dto = new _ModelOutputsDTO
      {
        ModelOutputs = modelOutputs
          .Map(mo => new _ModelOutputDTO
          {
            Name = mo.Name,
            ErrorModel = mo.ErrorModel.ToString()
          })
          .ToArray()
      };
      Toml.WriteFile(dto, pathToModelOutputs);
    }

    private static Arr<ModelOutput> LoadModelOutputs(DirectoryInfo chainStateDirectory)
    {
      var pathToModelOutputs = Combine(chainStateDirectory.FullName, $"{nameof(ModelOutputs).ToLowerInvariant()}.toml");
      if (!File.Exists(pathToModelOutputs)) return default;

      var modelOutputs = Toml.ReadFile<_ModelOutputsDTO>(pathToModelOutputs);

      RequireNotNull(modelOutputs.ModelOutputs);

      return modelOutputs.ModelOutputs
        .Select(dto => new ModelOutput(
          dto.Name.AssertNotNull(), 
          ErrorModel.DeserializeErrorModel(dto.ErrorModel.AssertNotNull()
          ).AssertSome()))
        .ToArr();
    }

    private static void SaveChainData(DataTable chainData, string pathToChainState)
    {
      var pathToChainData = Combine(pathToChainState, $"{nameof(ChainData).ToLowerInvariant()}.csv");
      SaveToCSV<double>(chainData, pathToChainData);
    }

    private static DataTable? LoadChainData(DirectoryInfo chainStateDirectory)
    {
      var pathToChainData = Combine(chainStateDirectory.FullName, $"{nameof(ChainData).ToLowerInvariant()}.csv");
      if (!File.Exists(pathToChainData)) return default;

      return LoadFromCSV<double>(pathToChainData);
    }

    private static void SaveErrorData(DataTable chainData, string pathToChainState)
    {
      var pathToErrorData = Combine(pathToChainState, $"{nameof(ErrorData).ToLowerInvariant()}.csv");
      SaveToCSV<double>(chainData, pathToErrorData);
    }

    private static void SavePosteriorData(IDictionary<string, DataTable> posteriorData, string pathToChainState)
    {
      foreach (var kvp in posteriorData)
      {
        var pathToPosteriorData = Combine(pathToChainState, $"{kvp.Key}.csv");
        SaveToCSV<double>(kvp.Value, pathToPosteriorData);
      }
    }

    private static DataTable? LoadErrorData(DirectoryInfo chainStateDirectory)
    {
      var pathToErrorData = Combine(chainStateDirectory.FullName, $"{nameof(ErrorData).ToLowerInvariant()}.csv");
      if (!File.Exists(pathToErrorData)) return default;

      return LoadFromCSV<double>(pathToErrorData);
    }

    private static IDictionary<string, DataTable>? LoadPosteriorData(DirectoryInfo chainStateDirectory, Arr<string> outputNames)
    {
      var loaded = outputNames
        .Map(n =>
        {
          var pathToPosteriorData = Combine(chainStateDirectory.FullName, $"{n}.csv");
          if (!File.Exists(pathToPosteriorData)) return None;

          return Some((Name: n, Posterior: LoadFromCSV<double>(pathToPosteriorData)));
        })
        .Somes()
        .ToArr();

      if (loaded.IsEmpty) return default;

      return loaded.ToDictionary(l => l.Name, l => l.Posterior);
    }
  }
}
