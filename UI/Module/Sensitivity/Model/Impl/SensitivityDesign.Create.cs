using LanguageExt;
using RVis.Model;
using System;
using static RVis.Base.Check;
using DataTable = System.Data.DataTable;

namespace Sensitivity
{
  internal sealed partial class SensitivityDesign
  {
    internal static SensitivityDesign CreateSensitivityDesign(
      DateTime createdOn,
      Arr<byte[]> serializedDesigns,
      Arr<(string Name, IDistribution Distribution)> parameterDistributions,
      SensitivityMethod sensitivityMethod,
      string methodParameters,
      Arr<DataTable> samples
      )
    {
      RequireFalse(parameterDistributions.IsEmpty);
      RequireTrue(parameterDistributions.ForAll(ps => ps.Distribution.IsConfigured));
      RequireNotNull(samples);
      RequireTrue(samples.Count > 0);
      RequireTrue(samples.ForAll(dt => dt.Rows.Count > 0));
      RequireTrue(parameterDistributions.ForAll(
        ps => 
          ps.Distribution.DistributionType == DistributionType.Invariant || 
          samples.ForAll(dt => dt.Columns.Contains(ps.Name))
        ));

      var designParameters = parameterDistributions.Map(
        ps => new DesignParameter(ps.Name, ps.Distribution)
        );

      return new SensitivityDesign(
        createdOn, 
        serializedDesigns, 
        designParameters, 
        sensitivityMethod, 
        methodParameters, 
        samples
        );
    }
  }
}
