using LanguageExt;
using MathNet.Numerics.Distributions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static Estimation.ErrorModel;
using static LanguageExt.Prelude;
using static MathNet.Numerics.Distributions.LogNormal;
using static RVis.Model.RandomNumberGenerator;
using static System.Double;
using static System.Globalization.CultureInfo;
using static System.Math;

namespace Estimation
{
  internal sealed class LogNormalErrorModel : IErrorModel, IEquatable<LogNormalErrorModel>
  {
    internal readonly static LogNormalErrorModel Default = new LogNormalErrorModel(
      NaN, 
      NaN, 
      DEFAULT_SIGMA_STEP_INITIALIZER
      );

    internal LogNormalErrorModel(double sigmaLog, double stepInitializer)
      : this(sigmaLog, NaN, stepInitializer)
    {
    }

    internal double SigmaLog { get; }

    internal double Step { get; }

    internal double StepInitializer { get; }

    public ErrorModelType ErrorModelType => ErrorModelType.LogNormal;

    public bool CanHandleNegativeSampleSpace => false;

    public bool IsConfigured => !IsNaN(SigmaLog);

    public double GetLogLikelihood(double mu, double x) =>
      PDFLn(Log(mu), SigmaLog, x);

    public double GetLogLikelihood(IEnumerable<double> mu, IEnumerable<double> x) =>
      mu
        .Zip(x)
        .Map(t => GetLogLikelihood(t.Item1, t.Item2))
        .Sum();

    public IErrorModel GetPerturbed(bool _)
    {
      var step = IsNaN(Step) ? SigmaLog * StepInitializer : Step;
      var perturbedSigmaLog = SigmaLog + step * Normal.Sample(Generator, 0d, 1d);
      return perturbedSigmaLog > 0d
        ? new LogNormalErrorModel(perturbedSigmaLog, step, StepInitializer)
        : this;
    }

    public IErrorModel ApplyBias(double bias) =>
      new LogNormalErrorModel(SigmaLog, Step * bias, StepInitializer);

    public bool Equals(LogNormalErrorModel? rhs) =>
      rhs is not null &&
      SigmaLog.Equals(rhs.SigmaLog) && 
      Step.Equals(rhs.Step) && 
      StepInitializer.Equals(rhs.StepInitializer);

    public override bool Equals(object? obj) =>
      obj is LogNormalErrorModel rhs && Equals(rhs);

    public static bool operator ==(LogNormalErrorModel left, LogNormalErrorModel right) =>
      left is null
        ? right is null
        : left.Equals(right);

    public static bool operator !=(LogNormalErrorModel left, LogNormalErrorModel right) =>
      !(left == right);

    public override int GetHashCode() => 
      HashCode.Combine(SigmaLog, Step, StepInitializer, ErrorModelType);

    public override string ToString() =>
      SerializeErrorModel(
        ErrorModelType,
        Array<object>(SigmaLog, Step, StepInitializer)
        );

    public string ToString(string variableName)
    {
      var variance = IsNaN(SigmaLog) ? "?" : (SigmaLog * SigmaLog).ToString("G4", CurrentCulture);
      return $"ln({variableName}) ~ N(µ, σ² = {variance})";
    }

    internal static Option<IErrorModel> Parse(Arr<string> parts)
    {
      if (parts.Count < 3) return None;
      if (!TryParse(parts[0], NumberStyles.Any, InvariantCulture, out double sigmaLog)) return None;
      if (!TryParse(parts[1], NumberStyles.Any, InvariantCulture, out double step)) return None;
      if (!TryParse(parts[2], NumberStyles.Any, InvariantCulture, out double stepInitializer)) return None;
      return new LogNormalErrorModel(sigmaLog, step, stepInitializer);
    }

    private LogNormalErrorModel(double sigmaLog, double step, double stepInitializer)
    {
      SigmaLog = sigmaLog;
      Step = step;
      StepInitializer = stepInitializer;
    }
  }
}
