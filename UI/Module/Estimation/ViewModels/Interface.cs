using LanguageExt;
using OxyPlot;
using RVisUI.AppInf;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Estimation
{
  public interface IIterationOptionsViewModel
  {
    string? IterationsToAddText { get; set; }
    int? IterationsToAdd { get; }
    string? TargetAcceptRateText { get; set; }
    double? TargetAcceptRate { get; }
    bool UseApproximation { get; set; }
    ICommand OK { get; }
    ICommand Cancel { get; }
    bool? DialogResult { get; set; }
  }

  internal interface IPriorViewModel
  {
    string Name { get; }
    string? Distribution { get; set; }
    bool IsSelected { get; set; }
    ICommand ToggleSelect { get; }
    string SortKey { get; }
  }

  internal interface IPriorsViewModel
  {
    Arr<IPriorViewModel> AllPriorViewModels { get; }
    ObservableCollection<IPriorViewModel> SelectedPriorViewModels { get; }
    int SelectedPriorViewModel { get; set; }
    IParameterDistributionViewModel ParameterDistributionViewModel { get; }
    bool IsVisible { get; set; }
  }

  internal interface IErrorViewModel : IDisposable
  {
    ErrorModelType ErrorModelType { get; }
    IErrorModel? ErrorModelUnsafe { get; set; }
    string? Variable { get; set; }
    string? Unit { get; set; }
  }

  internal interface IErrorViewModel<T> : IErrorViewModel where T : IErrorModel
  {
    Option<T> ErrorModel { get; set; }
  }

  internal interface INormalErrorViewModel : IErrorViewModel<NormalErrorModel>
  {
    double? Sigma { get; set; }
    double? StepInitializer { get; set; }
    double? Var { get; }
  }

  internal interface ILogNormalErrorViewModel : IErrorViewModel<LogNormalErrorModel>
  {
    double? SigmaLog { get; set; }
    double? StepInitializer { get; set; }
    double? VarLog { get; }
  }

  internal interface IHeteroscedasticPowerErrorViewModel : IErrorViewModel<HeteroscedasticPowerErrorModel>
  {
    double? Delta1 { get; set; }
    double? Delta1StepInitializer { get; set; }

    double? Delta2 { get; set; }
    double? Delta2StepInitializer { get; set; }

    double? Sigma { get; set; }
    double? SigmaStepInitializer { get; set; }
    double? Var { get; }

    double? Lower { get; set; }
  }

  internal interface IHeteroscedasticExpErrorViewModel : IErrorViewModel<HeteroscedasticExpErrorModel>
  {
    double? Delta { get; set; }
    double? DeltaStepInitializer { get; set; }

    double? Sigma { get; set; }
    double? SigmaStepInitializer { get; set; }
    double? Var { get; }

    double? Lower { get; set; }
  }

  internal interface IOutputErrorViewModel
  {
    Arr<string> ErrorModelNames { get; }
    int SelectedErrorModelName { get; set; }
    IErrorViewModel? ErrorViewModel { get; set; }
    Option<OutputState> OutputState { get; set; }
  }

  internal interface IOutputViewModel
  {
    string Name { get; }
    string? ErrorModel { get; set; }
    bool IsSelected { get; set; }
    ICommand ToggleSelect { get; }
    string SortKey { get; }
  }

  internal interface IObservationsViewModel
  {
    int ID { get; }
    string Subject { get; }
    string RefName { get; }
    string Source { get; }
    string Data { get; }
    bool IsSelected { get; set; }
  }

  internal interface ILikelihoodViewModel
  {
    Arr<IOutputViewModel> AllOutputViewModels { get; }
    ObservableCollection<IOutputViewModel> SelectedOutputViewModels { get; }
    int SelectedOutputViewModel { get; set; }
    IOutputErrorViewModel OutputErrorViewModel { get; }
    Arr<IObservationsViewModel> ObservationsViewModels { get; set; }
    PlotModel? PlotModel { get; set; }
    bool IsVisible { get; set; }
  }

  internal interface IDesignViewModel
  {
    Arr<string> Priors { get; }
    Arr<string> Invariants { get; }
    Arr<string> Outputs { get; }
    Arr<string> Observations { get; }

    int? Iterations { get; set; }
    int? BurnIn { get; set; }
    Arr<int> ChainsOptions { get; }
    int ChainsIndex { get; set; }

    ICommand CreateDesign { get; }
    bool CanCreateDesign { get; }

    DateTime? DesignCreatedOn { get; }
    ICommand UnloadDesign { get; }
    bool CanUnloadDesign { get; }

    bool IsSelected { get; set; }
  }

  internal interface ISimulationViewModel
  {
    ICommand StartIterating { get; }
    bool CanStartIterating { get; set; }
    ICommand StopIterating { get; }
    bool CanStopIterating { get; set; }
    ICommand ShowSettings { get; }
    bool CanShowSettings { get; set; }
    PlotModel PlotModel { get; }
    int? PosteriorBegin { get; set; }
    int? PosteriorEnd { get; set; }
    bool CanAdjustConvergenceRange { get; set; }
    ICommand SetConvergenceRange { get; }
    bool CanSetConvergenceRange { get; set; }
    Arr<string> Parameters { get; }
    int SelectedParameter { get; set; }

    bool IsVisible { get; set; }
  }

  internal interface IChainViewModel
  {
    int No { get; }
    bool IsSelected { get; set; }
  }

  internal interface IPosteriorViewModel
  {
    Arr<IChainViewModel> ChainViewModels { get; }
    Arr<string> ParameterNames { get; }
    int SelectedParameterName { get; set; }
    PlotModel PlotModel { get; }
    double? AcceptRate { get; set; }
    bool IsVisible { get; set; }
  }

  internal interface IFitViewModel
  {
    Arr<IChainViewModel> ChainViewModels { get; }
    Arr<string> OutputNames { get; }
    int SelectedOutputName { get; set; }
    PlotModel PlotModel { get; }
    bool IsVisible { get; set; }
  }

  internal interface IDesignDigestViewModel
  {
    DateTime CreatedOn { get; }
    string Description { get; }
  }

  internal interface IDesignDigestsViewModel
  {
    ObservableCollection<IDesignDigestViewModel> DesignDigestViewModels { get; }
    IDesignDigestViewModel? SelectedDesignDigestViewModel { get; set; }
    ICommand LoadEstimationDesign { get; }
    ICommand DeleteEstimationDesign { get; }
    ICommand FollowKeyboardInDesignDigests { get; }
    Option<(DateTime CreatedOn, DateTime SelectedOn)> TargetEstimationDesign { get; set; }
  }

  internal interface IViewModel
  {
    IPriorsViewModel PriorsViewModel { get; }
    ILikelihoodViewModel LikelihoodViewModel { get; }
    IDesignViewModel DesignViewModel { get; }
    ISimulationViewModel SimulationViewModel { get; }
    IPosteriorViewModel PosteriorViewModel { get; }
    IFitViewModel FitViewModel { get; }
    IDesignDigestsViewModel DesignDigestsViewModel { get; }
  }
}
