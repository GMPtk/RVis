using LanguageExt;
using static RVis.Base.Check;
using DataTable = System.Data.DataTable;

namespace Sensitivity
{
  internal sealed partial class SensitivityDesign
  {
    internal static void ExportMorris(
      SensitivityDesign sensitivityDesign,
      Map<string, (DataTable Mu, DataTable MuStar, DataTable Sigma)> outputMeasures,
      Arr<string> outputNames,
      string targetDirectory
      )
    {
      RequireTrue(outputNames.ForAll(outputMeasures.ContainsKey));

      SaveSamples(
        sensitivityDesign.Samples,
        targetDirectory
        );

      outputNames.Iter(on =>
      {
        var (mu, muStar, sigma) = outputMeasures[on];

        SaveMorrisOutputMeasures(
          on,
          mu,
          muStar,
          sigma,
          targetDirectory
          );
      });
    }

    internal static void ExportFast99(
      SensitivityDesign sensitivityDesign,
      Map<string, (DataTable FirstOrder, DataTable TotalOrder, DataTable Variance)> outputMeasures,
      Arr<string> outputNames,
      string targetDirectory
      )
    {
      RequireTrue(outputNames.ForAll(outputMeasures.ContainsKey));

      SaveSamples(
        sensitivityDesign.Samples,
        targetDirectory
        );

      outputNames.Iter(on =>
      {
        var (firstOrder, totalOrder, variance) = outputMeasures[on];

        SaveFast99OutputMeasures(
          on,
          firstOrder,
          totalOrder,
          variance,
          targetDirectory
          );
      });
    }
  }
}
