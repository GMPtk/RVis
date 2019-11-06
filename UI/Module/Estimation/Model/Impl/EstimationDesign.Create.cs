using LanguageExt;
using RVis.Model;
using System;
using System.Linq;
using static RVis.Base.Check;

namespace Estimation
{
  internal sealed partial class EstimationDesign
  {
    internal static EstimationDesign CreateEstimationDesign(
      DateTime createdOn,
      Arr<(string Name, IDistribution Distribution)> priorDistributions,
      Arr<(string Name, IErrorModel ErrorModel)> outputErrorModels,
      Arr<SimObservations> observations,
      int iterations,
      int burnIn,
      int chains
      )
    {
      RequireFalse(priorDistributions.IsEmpty);
      RequireTrue(priorDistributions.ForAll(pd => pd.Distribution.IsConfigured));

      RequireFalse(outputErrorModels.IsEmpty);
      RequireTrue(outputErrorModels.ForAll(oem => oem.ErrorModel.IsConfigured));

      RequireTrue(outputErrorModels.ForAll(oem => observations.Exists(o => o.Subject == oem.Name)));

      var priors = priorDistributions
        .OrderBy(pd => pd.Name)
        .Select(dp => new ModelParameter(dp.Name, dp.Distribution))
        .ToArr();

      var outputs = outputErrorModels
        .OrderBy(oem => oem.Name)
        .Select(oem => new ModelOutput(oem.Name, oem.ErrorModel))
        .ToArr();

      return new EstimationDesign(
        createdOn,
        priors,
        outputs,
        observations,
        iterations,
        burnIn,
        chains,
        targetAcceptRate: default,
        useApproximation: true
        );
    }
  }
}
