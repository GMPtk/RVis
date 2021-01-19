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
  internal sealed class HeteroscedasticExpErrorModel : IErrorModel, IEquatable<HeteroscedasticExpErrorModel>
  {
    internal readonly static HeteroscedasticExpErrorModel Default = 
      new HeteroscedasticExpErrorModel(
        0d, 
        NaN, 
        DEFAULT_DELTA_STEP_INITIALIZER, 
        NaN, 
        NaN, 
        DEFAULT_SIGMA_STEP_INITIALIZER, 
        0d
        );

    internal HeteroscedasticExpErrorModel(
      double delta, 
      double deltaStepInitializer, 
      double sigma,
      double sigmaStepInitializer,
      double lower
      )
      : this(
          delta, 
          NaN, 
          deltaStepInitializer, 
          sigma, 
          NaN, 
          sigmaStepInitializer, 
          lower
          )
    {
    }

    internal double Delta { get; }
    internal double DeltaStep { get; }
    internal double DeltaStepInitializer { get; }

    internal double Sigma { get; }
    internal double SigmaStep { get; }
    internal double SigmaStepInitializer { get; }

    internal double Lower { get; }

    public ErrorModelType ErrorModelType => ErrorModelType.HeteroscedasticExp;

    public bool CanHandleNegativeSampleSpace => true;

    public bool IsConfigured =>
      !IsNaN(Delta) &&
      !IsNaN(DeltaStepInitializer) &&
      !IsNaN(Sigma) &&
      !IsNaN(SigmaStepInitializer) &&
      !IsNaN(Lower);

    public double GetLogLikelihood(double mu, double x)
    {
      var variance = Sigma * Sigma * Exp(2d * Delta * mu);
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
          ? new HeteroscedasticExpErrorModel(
              Delta, 
              DeltaStep, 
              DeltaStepInitializer, 
              perturbedSigma, 
              sigmaStep, 
              SigmaStepInitializer, 
              Lower
              )
          : this;
      }

      var deltaStep = IsNaN(DeltaStep) ? Delta * DeltaStepInitializer : DeltaStep;
      var perturbedDelta = Delta + deltaStep * Sample(Generator, 0d, 1d);

      return new HeteroscedasticExpErrorModel(
        perturbedDelta,
        deltaStep,
        DeltaStepInitializer,
        perturbedSigma > 0d ? perturbedSigma : Sigma,
        sigmaStep,
        SigmaStepInitializer,
        Lower
        );

    }

    public IErrorModel ApplyBias(double bias) =>
      new HeteroscedasticExpErrorModel(
        Delta,
        DeltaStep,
        DeltaStepInitializer,
        Sigma,
        SigmaStep * Sqrt(bias),
        SigmaStepInitializer,
        Lower
        );

    public bool Equals(HeteroscedasticExpErrorModel? rhs) =>
      rhs is not null &&
      Delta.Equals(rhs.Delta) && DeltaStep.Equals(rhs.DeltaStep) && DeltaStepInitializer.Equals(rhs.DeltaStepInitializer) &&
      Sigma.Equals(rhs.Sigma) && SigmaStep.Equals(rhs.SigmaStep) && SigmaStepInitializer.Equals(rhs.SigmaStepInitializer) &&
      Lower.Equals(rhs.Lower);

    public override bool Equals(object? obj) =>
      obj is HeteroscedasticExpErrorModel rhs && Equals(rhs);

    public static bool operator ==(HeteroscedasticExpErrorModel left, HeteroscedasticExpErrorModel right) =>
      left is null
        ? right is null
        : left.Equals(right);

    public static bool operator !=(HeteroscedasticExpErrorModel left, HeteroscedasticExpErrorModel right) =>
      !(left == right);

    public override int GetHashCode() => 
      HashCode.Combine(Delta, DeltaStep, DeltaStepInitializer, Sigma, SigmaStep, SigmaStepInitializer, Lower, ErrorModelType);

    public override string ToString() =>
      SerializeErrorModel(
        ErrorModelType,
        Array<object>(
          Delta, 
          DeltaStep, 
          DeltaStepInitializer, 
          Sigma, 
          SigmaStep, 
          SigmaStepInitializer, 
          Lower
          )
        );

    public string ToString(string variableName)
    {
      var variance = IsNaN(Sigma) ? "?" : (Sigma * Sigma).ToString("G4", CurrentCulture);
      var delta = IsNaN(Delta) ? "?" : Delta.ToString("G4", CurrentCulture);

      return $"{variableName} ~ fN(µ, σ² = {variance}, δ = {delta})";
    }

    internal static Option<IErrorModel> Parse(Arr<string> parts)
    {
      if (parts.Count < 7) return None;

      if (!TryParse(parts[0], NumberStyles.Any, InvariantCulture, out double delta)) return None;
      if (!TryParse(parts[1], NumberStyles.Any, InvariantCulture, out double deltaStep)) return None;
      if (!TryParse(parts[2], NumberStyles.Any, InvariantCulture, out double deltaStepInitializer)) return None;
      if (!TryParse(parts[3], NumberStyles.Any, InvariantCulture, out double sigma)) return None;
      if (!TryParse(parts[4], NumberStyles.Any, InvariantCulture, out double sigmaStep)) return None;
      if (!TryParse(parts[5], NumberStyles.Any, InvariantCulture, out double sigmaStepInitializer)) return None;
      if (!TryParse(parts[6], NumberStyles.Any, InvariantCulture, out double lower)) return None;

      return new HeteroscedasticExpErrorModel(
        delta,
        deltaStep,
        deltaStepInitializer,
        sigma, 
        sigmaStep,
        sigmaStepInitializer,
        lower
        );
    }

    private HeteroscedasticExpErrorModel(
      double delta,
      double deltaStep,
      double deltaStepInitializer,
      double sigma,
      double sigmaStep,
      double sigmaStepInitializer,
      double lower
      )
    {
      Delta = delta;
      DeltaStep = deltaStep;
      DeltaStepInitializer = deltaStepInitializer;

      Sigma = sigma;
      SigmaStep = sigmaStep;
      SigmaStepInitializer = sigmaStepInitializer;

      Lower = lower;
    }

    private double GetLogLikelihood((double Mu, double X) t) =>
      GetLogLikelihood(t.Mu, t.X);
  }
}
