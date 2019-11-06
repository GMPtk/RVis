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
      byte[] serializedDesign,
      Arr<(string Name, IDistribution Distribution)> parameterDistributions,
      int sampleSize,
      DataTable samples
      )
    {
      RequireFalse(parameterDistributions.IsEmpty);
      RequireTrue(parameterDistributions.ForAll(ps => ps.Distribution.IsConfigured));
      RequireNotNull(samples);
      RequireTrue(samples.Columns.Count > 0);
      RequireTrue(samples.Rows.Count > 0);
      RequireTrue(parameterDistributions.ForAll(
        ps => ps.Distribution.DistributionType == DistributionType.Invariant || samples.Columns.Contains(ps.Name)
        ));

      var designParameters = parameterDistributions.Map(ps => new DesignParameter(ps.Name, ps.Distribution));

      return new SensitivityDesign(createdOn, serializedDesign, designParameters, sampleSize, samples);
    }
  }
}
