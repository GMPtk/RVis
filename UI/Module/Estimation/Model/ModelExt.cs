using LanguageExt;
using System;
using System.Collections.Generic;
using static System.Globalization.CultureInfo;
using static System.String;

namespace Estimation
{
  internal static class ModelExt
  {
    internal static string ToDirectoryName(this DateTime dateTime) =>
      dateTime.ToString("yyyyMMddHHmmss", InvariantCulture);

    internal static DateTime FromDirectoryName(this string directoryName) =>
      DateTime.ParseExact(directoryName, "yyyyMMddHHmmss", InvariantCulture);

    internal static string GetDescription(this EstimationDesign estimationDesign) =>
      GetDescription(
        estimationDesign.Priors,
        estimationDesign.Outputs,
        estimationDesign.Iterations,
        estimationDesign.BurnIn,
        estimationDesign.Chains
        );

    internal static string GetDescription(
      Arr<ModelParameter> priors,
      Arr<ModelOutput> outputs,
      int iterations,
      int burnIn,
      int chains
      )
    {
      var parts = new List<string>
      {
        $"iter = {iterations}",
        $"burn in = {burnIn}",
        $"chains = {chains}"
      };

      parts.AddRange(priors.Map(
        dp => dp.Distribution.ToString(dp.Name)
        ));

      parts.AddRange(outputs.Map(
        @do => @do.ErrorModel.ToString(@do.Name)
        ));

      return Join(", ", parts);
    }

    internal static Option<DesignDigest> GetDesignDigest(this EstimationDesigns estimationDesigns, DateTime createdOn) =>
      estimationDesigns.DesignDigests.Find(dd => dd.CreatedOn == createdOn);
  }
}
