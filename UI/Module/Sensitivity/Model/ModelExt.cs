using LanguageExt;
using System;
using System.Collections.Generic;
using System.Linq;
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

    internal static string FromMethodParameters(
      this Arr<(string Name, object Value)> methodParameters
      ) =>
      Join(";", methodParameters.Map(mp => $"{mp.Name}={mp.Value}"));

    internal static Arr<(string Name, string Value)> ToMethodParameters(
      this string methodParameters
      ) =>
      methodParameters.Split(';').Select(s => s.Split('=').ToMethodParameter()).ToArr();

    internal static string GetDescription(this SensitivityDesign sensitivityDesign) =>
      GetDescription(
        sensitivityDesign.SensitivityMethod, 
        sensitivityDesign.MethodParameters.ToMethodParameters(), 
        sensitivityDesign.DesignParameters
        );

    internal static string GetDescription(
      SensitivityMethod sensitivityMethod, 
      Arr<(string Name, string Value)> methodParameters, 
      Arr<DesignParameter> designParameters
      )
    {
      var parts = new List<string>
      {
        $"method = {sensitivityMethod}"
      };

      parts.AddRange(methodParameters.Map(mp => $"{mp.Name} = {mp.Value}"));

      parts.AddRange(designParameters.Map(
        dp => dp.Distribution.ToString(dp.Name)
        ));

      return Join(", ", parts);
    }

    internal static Option<DesignDigest> GetDesignDigest(
      this SensitivityDesigns sensitivityDesigns, 
      DateTime createdOn
      ) =>
      sensitivityDesigns.DesignDigests.Find(dd => dd.CreatedOn == createdOn);

    private static (string Name, string Value) ToMethodParameter(
      this string[] methodParameterParts
      ) =>
      (methodParameterParts[0], methodParameterParts[1]);
  }
}
