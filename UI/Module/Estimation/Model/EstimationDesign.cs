using LanguageExt;
using RVis.Model;
using System;

namespace Estimation
{
  internal sealed partial class EstimationDesign
  {
    internal EstimationDesign(
      DateTime createdOn,
      Arr<ModelParameter> priors,
      Arr<ModelOutput> outputs,
      Arr<SimObservations> observations,
      int iterations,
      int burnIn,
      int chains,
      double? targetAcceptRate,
      bool useApproximation
      )
    {
      CreatedOn = createdOn;

      Priors = priors;
      Outputs = outputs;
      Observations = observations;

      Iterations = iterations;
      BurnIn = burnIn;
      Chains = chains;
      TargetAcceptRate = targetAcceptRate;
      UseApproximation = useApproximation;
    }

    internal DateTime CreatedOn { get; }

    internal Arr<ModelParameter> Priors { get; }
    internal Arr<ModelOutput> Outputs { get; }
    internal Arr<SimObservations> Observations { get; }

    internal int Iterations { get; }
    internal int BurnIn { get; }
    internal int Chains { get; }
    internal double? TargetAcceptRate { get; }
    internal bool UseApproximation { get; }

    public override string ToString() => this.GetDescription();
  }
}
