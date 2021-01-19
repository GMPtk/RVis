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

namespace Estimation
{
  internal sealed class NormalErrorModel : IErrorModel, IEquatable<NormalErrorModel>
  {
    internal readonly static NormalErrorModel Default = new NormalErrorModel(
      NaN, 
      NaN, 
      DEFAULT_SIGMA_STEP_INITIALIZER
      );

    internal NormalErrorModel(double sigma, double stepInitializer)
      : this(sigma, NaN, stepInitializer)
    {
    }

    internal double Sigma { get; }

    internal double Step { get; }

    internal double StepInitializer { get; }

    public ErrorModelType ErrorModelType => ErrorModelType.Normal;

    public bool CanHandleNegativeSampleSpace => true;

    public bool IsConfigured => !IsNaN(Sigma) && !IsNaN(StepInitializer);

    public double GetLogLikelihood(double mu, double x) =>
      PDFLn(mu, Sigma, x);

    public double GetLogLikelihood(IEnumerable<double> mu, IEnumerable<double> x) =>
      mu
        .Zip(x)
        .Map(t => GetLogLikelihood(t.Item1, t.Item2))
        .Sum();

    public IErrorModel GetPerturbed(bool _)
    {
      var step = IsNaN(Step) ? Sigma * StepInitializer : Step;
      var perturbedSigma = Sigma + step * Sample(Generator, 0d, 1d);
      return perturbedSigma > 0d
        ? new NormalErrorModel(perturbedSigma, step, StepInitializer)
        : this;
    }

    public IErrorModel ApplyBias(double bias) => 
      new NormalErrorModel(Sigma, Step * bias, StepInitializer);

    public bool Equals(NormalErrorModel? rhs) =>
      rhs is not null &&
      Sigma.Equals(rhs.Sigma) && 
      Step.Equals(rhs.Step) && 
      StepInitializer.Equals(rhs.StepInitializer);

    public override bool Equals(object? obj) =>
      obj is NormalErrorModel rhs && Equals(rhs);

    public static bool operator ==(NormalErrorModel left, NormalErrorModel right) =>
      left is null
        ? right is null
        : left.Equals(right);

    public static bool operator !=(NormalErrorModel left, NormalErrorModel right) =>
      !(left == right);

    public override int GetHashCode() => 
      HashCode.Combine(Sigma, Step, StepInitializer, ErrorModelType);

    public override string ToString() =>
      SerializeErrorModel(
        ErrorModelType,
        Array<object>(Sigma, Step, StepInitializer)
        );

    public string ToString(string variableName)
    {
      var variance = IsNaN(Sigma) ? "?" : (Sigma * Sigma).ToString("G4", CurrentCulture);
      return $"{variableName} ~ N(µ, σ² = {variance})";
    }

    internal static Option<IErrorModel> Parse(Arr<string> parts)
    {
      if (parts.Count < 3) return None;
      if (!TryParse(parts[0], NumberStyles.Any, InvariantCulture, out double sigma)) return None;
      if (!TryParse(parts[1], NumberStyles.Any, InvariantCulture, out double step)) return None;
      if (!TryParse(parts[2], NumberStyles.Any, InvariantCulture, out double stepInitializer)) return None;
      return new NormalErrorModel(sigma, step, stepInitializer);
    }

    private NormalErrorModel(double sigma, double step, double stepInitializer)
    {
      Sigma = sigma;
      Step = step;
      StepInitializer = stepInitializer;
    }
  }
}
