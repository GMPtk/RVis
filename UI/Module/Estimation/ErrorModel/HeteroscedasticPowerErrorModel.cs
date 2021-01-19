using LanguageExt;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static Estimation.ErrorModel;
using static LanguageExt.Prelude;
using static MathNet.Numerics.Distributions.Normal;
using static RVis.Model.RandomNumberGenerator;
using static System.Double;
using static System.Globalization.CultureInfo;
using static System.Math;
using static Estimation.TruncNorm;

namespace Estimation
{
  internal sealed class HeteroscedasticPowerErrorModel : IErrorModel, IEquatable<HeteroscedasticPowerErrorModel>
  {
    internal readonly static HeteroscedasticPowerErrorModel Default = 
      new HeteroscedasticPowerErrorModel(
        1d, 
        NaN, 
        DEFAULT_DELTA_STEP_INITIALIZER, 
        -10d, 
        NaN, 
        DEFAULT_DELTA_STEP_INITIALIZER, 
        NaN, 
        NaN, 
        DEFAULT_SIGMA_STEP_INITIALIZER, 
        0d
        );

    internal HeteroscedasticPowerErrorModel(
      double delta1, 
      double delta1StepInitializer, 
      double delta2, 
      double delta2StepInitializer, 
      double sigma,
      double sigmaStepInitializer,
      double lower
      )
      : this(
          delta1, 
          NaN, 
          delta1StepInitializer, 
          delta2, 
          NaN, 
          delta2StepInitializer, 
          sigma, 
          NaN, 
          sigmaStepInitializer, 
          lower
          )
    {
    }

    internal double Delta1 { get; }
    internal double Delta1Step { get; }
    internal double Delta1StepInitializer { get; }
    
    internal double Delta2 { get; }
    internal double Delta2Step { get; }
    internal double Delta2StepInitializer { get; }

    internal double Sigma { get; }
    internal double SigmaStep { get; }
    internal double SigmaStepInitializer { get; }

    internal double Lower { get; }

    public ErrorModelType ErrorModelType => ErrorModelType.HeteroscedasticPower;

    public bool CanHandleNegativeSampleSpace => true;

    public bool IsConfigured =>
      !IsNaN(Delta1) &&
      !IsNaN(Delta1StepInitializer) &&
      !IsNaN(Delta2) &&
      !IsNaN(Delta2StepInitializer) &&
      !IsNaN(Sigma) &&
      !IsNaN(SigmaStepInitializer) &&
      !IsNaN(Lower);

    public double GetLogLikelihood(double mu, double x)
    {
      var variance = Sigma * Sigma * Pow(Delta1 + Pow(Abs(mu), Delta2), 2);
      var density = DTruncNorm(x, Lower, PositiveInfinity, mu, Sqrt(variance));
      return Log(density);
    }

    public double GetLogLikelihood(IEnumerable<double> mu, IEnumerable<double> x) =>
      mu
        .Zip(x)
        .Map(GetLogLikelihood)
        .Sum();

    public IErrorModel GetPerturbed(bool isBurnIn)
    {
      var sigmaStep = IsNaN(SigmaStep) ? Sigma * SigmaStepInitializer : SigmaStep;
      var perturbedSigma = Sigma + sigmaStep * Sample(Generator, 0d, 1d);

      if (isBurnIn)
      {
        return perturbedSigma > 0d
          ? new HeteroscedasticPowerErrorModel(
              Delta1, 
              Delta1Step, 
              Delta1StepInitializer, 
              Delta2, 
              Delta2Step, 
              Delta2StepInitializer, 
              perturbedSigma, 
              sigmaStep, 
              SigmaStepInitializer, 
              Lower
              )
          : this;
      }

      var delta1Step = IsNaN(Delta1Step) ? Delta1 * Delta1StepInitializer : Delta1Step;
      var perturbedDelta1 = Delta1 + delta1Step * Sample(Generator, 0d, 1d);

      var delta2Step = IsNaN(Delta2Step) ? Delta2 * Delta2StepInitializer : Delta2Step;
      var perturbedDelta2 = Delta2 + delta2Step * Sample(Generator, 0d, 1d);

      return new HeteroscedasticPowerErrorModel(
        perturbedDelta1,
        delta1Step,
        Delta1StepInitializer,
        perturbedDelta2,
        delta2Step,
        Delta2StepInitializer,
        perturbedSigma > 0d ? perturbedSigma : Sigma,
        sigmaStep,
        SigmaStepInitializer,
        Lower
        );

    }

    public IErrorModel ApplyBias(double bias) =>
      new HeteroscedasticPowerErrorModel(
        Delta1,
        Delta1Step,
        Delta1StepInitializer,
        Delta2, 
        Delta2Step,
        Delta2StepInitializer,
        Sigma,
        SigmaStep * Sqrt(bias),
        SigmaStepInitializer,
        Lower
        );

    public bool Equals(HeteroscedasticPowerErrorModel? rhs) =>
      rhs is not null &&
      Delta1.Equals(rhs.Delta1) && Delta1Step.Equals(rhs.Delta1Step) && Delta1StepInitializer.Equals(rhs.Delta1StepInitializer) &&
      Delta2.Equals(rhs.Delta2) && Delta2Step.Equals(rhs.Delta2Step) && Delta2StepInitializer.Equals(rhs.Delta2StepInitializer) &&
      Sigma.Equals(rhs.Sigma) && SigmaStep.Equals(rhs.SigmaStep) && SigmaStepInitializer.Equals(rhs.SigmaStepInitializer) &&
      Lower.Equals(rhs.Lower);

    public override bool Equals(object? obj) =>
      obj is HeteroscedasticPowerErrorModel rhs && Equals(rhs);

    public static bool operator ==(HeteroscedasticPowerErrorModel left, HeteroscedasticPowerErrorModel right) =>
      left is null
        ? right is null
        : left.Equals(right);

    public static bool operator !=(HeteroscedasticPowerErrorModel left, HeteroscedasticPowerErrorModel right) =>
      !(left == right);

    public override int GetHashCode()
    {
      var hash = new HashCode();
      hash.Add(Delta1);
      hash.Add(Delta1Step);
      hash.Add(Delta1StepInitializer);
      hash.Add(Delta2);
      hash.Add(Delta2Step);
      hash.Add(Delta2StepInitializer);
      hash.Add(Sigma);
      hash.Add(SigmaStep);
      hash.Add(SigmaStepInitializer);
      hash.Add(Lower);
      hash.Add(ErrorModelType);
      return hash.ToHashCode();
    }

    public override string ToString() =>
      SerializeErrorModel(
        ErrorModelType,
        Array<object>(
          Delta1, 
          Delta1Step, 
          Delta1StepInitializer, 
          Delta2, 
          Delta2Step, 
          Delta2StepInitializer, 
          Sigma, 
          SigmaStep, 
          SigmaStepInitializer, 
          Lower
          )
        );

    public string ToString(string variableName)
    {
      var variance = IsNaN(Sigma) ? "?" : (Sigma * Sigma).ToString("G4", CurrentCulture);
      var delta1 = IsNaN(Delta1) ? "?" : Delta1.ToString("G4", CurrentCulture);
      var delta2 = IsNaN(Delta2) ? "?" : Delta2.ToString("G4", CurrentCulture);

      return $"{variableName} ~ fN(µ, σ² = {variance}, δ1 = {delta1}, δ2 = {delta2})";
    }

    internal static Option<IErrorModel> Parse(Arr<string> parts)
    {
      if (parts.Count < 10) return None;

      if (!TryParse(parts[0], NumberStyles.Any, InvariantCulture, out double delta1)) return None;
      if (!TryParse(parts[1], NumberStyles.Any, InvariantCulture, out double delta1Step)) return None;
      if (!TryParse(parts[2], NumberStyles.Any, InvariantCulture, out double delta1StepInitializer)) return None;
      if (!TryParse(parts[3], NumberStyles.Any, InvariantCulture, out double delta2)) return None;
      if (!TryParse(parts[4], NumberStyles.Any, InvariantCulture, out double delta2Step)) return None;
      if (!TryParse(parts[5], NumberStyles.Any, InvariantCulture, out double delta2StepInitializer)) return None;
      if (!TryParse(parts[6], NumberStyles.Any, InvariantCulture, out double sigma)) return None;
      if (!TryParse(parts[7], NumberStyles.Any, InvariantCulture, out double sigmaStep)) return None;
      if (!TryParse(parts[8], NumberStyles.Any, InvariantCulture, out double sigmaStepInitializer)) return None;
      if (!TryParse(parts[9], NumberStyles.Any, InvariantCulture, out double lower)) return None;

      return new HeteroscedasticPowerErrorModel(
        delta1,
        delta1Step,
        delta1StepInitializer,
        delta2, 
        delta2Step,
        delta2StepInitializer,
        sigma, 
        sigmaStep,
        sigmaStepInitializer,
        lower
        );
    }

    private HeteroscedasticPowerErrorModel(
      double delta1,
      double delta1Step,
      double delta1StepInitializer,
      double delta2,
      double delta2Step,
      double delta2StepInitializer,
      double sigma,
      double sigmaStep,
      double sigmaStepInitializer,
      double lower
      )
    {
      Delta1 = delta1;
      Delta1Step = delta1Step;
      Delta1StepInitializer = delta1StepInitializer;

      Delta2 = delta2;
      Delta2Step = delta2Step;
      Delta2StepInitializer = delta2StepInitializer;

      Sigma = sigma;
      SigmaStep = sigmaStep;
      SigmaStepInitializer = sigmaStepInitializer;

      Lower = lower;
    }

    private double GetLogLikelihood((double Mu, double X) t) =>
      GetLogLikelihood(t.Mu, t.X);
  }
}
