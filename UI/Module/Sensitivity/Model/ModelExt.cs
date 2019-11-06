using LanguageExt;
using System;
using System.Collections.Generic;
using static System.Globalization.CultureInfo;
using static System.String;

namespace Sensitivity
{
  internal static class ModelExt
  {
    internal static string ToDirectoryName(this DateTime dateTime) =>
      dateTime.ToString("yyyyMMddHHmmss", InvariantCulture);

    internal static DateTime FromDirectoryName(this string directoryName) =>
      DateTime.ParseExact(directoryName, "yyyyMMddHHmmss", InvariantCulture);

    internal static string GetDescription(this SensitivityDesign sensitivityDesign) =>
      GetDescription(sensitivityDesign.DesignParameters, sensitivityDesign.SampleSize);

    internal static string GetDescription(Arr<DesignParameter> designParameters, int sampleSize)
    {
      var parts = new List<string>
      {
        $"n = {sampleSize}"
      };

      parts.AddRange(designParameters.Map(
        dp => dp.Distribution.ToString(dp.Name)
        ));

      return Join(", ", parts);
    }

    internal static Option<DesignDigest> GetDesignDigest(this SensitivityDesigns sensitivityDesigns, DateTime createdOn) =>
      sensitivityDesigns.DesignDigests.Find(dd => dd.CreatedOn == createdOn);
  }
}
