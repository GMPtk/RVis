using LanguageExt;
using OxyPlot;
using RVis.Model;
using System;

namespace RVisUI.AppInf
{
  public interface IDistributionViewModel : IDisposable
  {
    DistributionType DistributionType { get; }
    bool AllowTruncation { get; set; }
    IDistribution? DistributionUnsafe { get; set; }
    string? Variable { get; set; }
    string? Unit { get; set; }
    PlotModel PlotModel { get; }
  }

  public interface IDistributionViewModel<T> : IDistributionViewModel where T : IDistribution
  {
    Option<T> Distribution { get; set; }
  }

  public interface IInvariantDistributionViewModel : IDistributionViewModel<InvariantDistribution>
  {
    double? Value { get; set; }
  }

  public interface INormalDistributionViewModel : IDistributionViewModel<NormalDistribution>
  {
    double? Mu { get; set; }
    double? Sigma { get; set; }
    double? Var { get; }
    double? Lower { get; set; }
    double? Upper { get; set; }
  }

  public interface ILogNormalDistributionViewModel : IDistributionViewModel<LogNormalDistribution>
  {
    double? Mu { get; set; }
    double? Sigma { get; set; }
    double? Var { get; }
    double? Lower { get; set; }
    double? Upper { get; set; }
  }

  public interface IUniformDistributionViewModel : IDistributionViewModel<UniformDistribution>
  {
    double? Lower { get; set; }
    double? Upper { get; set; }
  }

  public interface IBetaDistributionViewModel : IDistributionViewModel<BetaDistribution>
  {
    double? Alpha { get; set; }
    double? Beta { get; set; }
    double? Lower { get; set; }
    double? Upper { get; set; }
  }

  public interface IBetaScaledDistributionViewModel : IDistributionViewModel<BetaScaledDistribution>
  {
    double? Alpha { get; set; }
    double? Beta { get; set; }
    double? Location { get; set; }
    double? Scale { get; set; }
    double? Lower { get; set; }
    double? Upper { get; set; }
  }

  public interface ILogUniformDistributionViewModel : IDistributionViewModel<LogUniformDistribution>
  {
    double? Lower { get; set; }
    double? Upper { get; set; }
  }

  public interface IGammaDistributionViewModel : IDistributionViewModel<GammaDistribution>
  {
    double? Alpha { get; set; }
    double? Beta { get; set; }
    double? Lower { get; set; }
    double? Upper { get; set; }
  }

  public interface IInverseGammaDistributionViewModel : IDistributionViewModel<InverseGammaDistribution>
  {
    double? Alpha { get; set; }
    double? Beta { get; set; }
  }
  public interface IStudentTDistributionViewModel : IDistributionViewModel<StudentTDistribution>
  {
    double? Mu { get; set; }
    double? Sigma { get; set; }
    double? Nu { get; }
    double? Lower { get; set; }
    double? Upper { get; set; }
  }
}
