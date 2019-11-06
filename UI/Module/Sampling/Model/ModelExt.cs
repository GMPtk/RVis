using LanguageExt;
using System;
using System.Collections.Generic;
using System.Data;
using static System.Globalization.CultureInfo;
using static System.String;

namespace Sampling
{
  internal static class ModelExt
  {
    internal static string ToDirectoryName(this DateTime dateTime) =>
      dateTime.ToString("yyyyMMddHHmmss", InvariantCulture);

    internal static DateTime FromDirectoryName(this string directoryName) =>
      DateTime.ParseExact(directoryName, "yyyyMMddHHmmss", InvariantCulture);

    internal static string GetDescription(this SamplingDesign samplingDesign) =>
      GetDescription(samplingDesign.DesignParameters, samplingDesign.Seed, samplingDesign.Samples, samplingDesign.NoDataIndices);

    internal static string GetDescription(Arr<DesignParameter> designParameters, int? seed, DataTable samples, Arr<int> noDataIndices)
    {
      var parts = new List<string>();

      if (samples.Rows.Count > 0)
      {
        parts.Add($"n = {samples.Rows.Count}");
      }

      if (seed.HasValue)
      {
        parts.Add($"seed = {seed.Value}");
      }

      if (noDataIndices.Count > 0)
      {
        parts.Add($"faults = {noDataIndices.Count}");
      }

      if (designParameters.Count > 0)
      {
        parts.AddRange(designParameters.Map(
          dp => dp.Distribution.ToString(dp.Name)
          ));
      }

      return Join(", ", parts);
    }

    internal static Option<DesignDigest> GetDesignDigest(this SamplingDesigns samplingDesigns, DateTime createdOn) =>
      samplingDesigns.DesignDigests.Find(dd => dd.CreatedOn == createdOn);
  }
}
