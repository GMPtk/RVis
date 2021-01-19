using LanguageExt;
using RVis.Base.Extensions;
using RVis.Model;
using System.Data;
using System.Linq;
using static System.Double;

namespace Estimation
{
  internal static class StateExt
  {
    public static int GetCompletedIterations(this ChainState chainState) =>
      chainState.ChainData.AssertNotNull().Rows
        .Cast<DataRow>()
        .Count(dr => chainState.ModelParameters.All(mp => !IsNaN(dr.Field<double>(mp.Name))));

    public static OutputState WithIsSelected(this OutputState outputState, bool isSelected) =>
      new OutputState(outputState.Name, outputState.ErrorModelType, outputState.ErrorModels, isSelected);

    public static IErrorModel GetErrorModel(this OutputState outputState) =>
      outputState.ErrorModels
        .Find(d => d.ErrorModelType == outputState.ErrorModelType)
        .AssertSome($"Failed to find {outputState.ErrorModelType} in error models for {outputState.Name}");

    public static IErrorModel GetErrorModel(this OutputState outputState, ErrorModelType errorModelType) =>
      outputState.ErrorModels
        .Find(d => d.ErrorModelType == errorModelType)
        .AssertSome($"Failed to find {errorModelType} in error models for {outputState.Name}");

    public static Arr<SimObservations> GetRelevantObservations(this ModuleState moduleState)
    {
      var selectedOutputStates = moduleState.OutputStates.Filter(os => os.IsSelected);

      return moduleState.SelectedObservations.Filter(
        o => selectedOutputStates.Exists(os => os.Name == o.Subject)
        );
    }
  }
}
