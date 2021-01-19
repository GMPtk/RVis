using LanguageExt;
using OxyPlot;
using RVis.Model;
using RVisUI.AppInf;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Windows.Input;

namespace Sampling
{
  internal interface IParameterSamplingViewModel
  {
    SimParameter Parameter { get; }
    string SortKey { get; }
    IDistribution? Distribution { get; set; }
    Arr<double> Samples { get; set; }
    PlotModel Histogram { get; }
  }

  internal interface IParameterViewModel
  {
    string Name { get; }
    string? Distribution { get; set; }
    bool IsSelected { get; set; }
    ICommand? ToggleSelect { get; }
    string SortKey { get; }
  }

  internal interface IParametersViewModel
  {
    bool IsVisible { get; set; }
    Arr<IParameterViewModel> AllParameterViewModels { get; }
    ObservableCollection<IParameterViewModel> SelectedParameterViewModels { get; }
    int SelectedParameterViewModel { get; set; }
    IParameterDistributionViewModel ParameterDistributionViewModel { get; }
  }

  internal interface ILHSConfigurationViewModel
  {
    bool IsDiceDesignInstalled { get; }

    LatinHypercubeDesignType LatinHypercubeDesignType { get; set; }

    bool UseSimulatedAnnealing { get; set; }
    double? T0 { get; set; }
    double? C { get; set; }
    int? Iterations { get; set; }
    double? P { get; set; }
    TemperatureDownProfile Profile { get; set; }
    int? Imax { get; set; }

    ICommand Disable { get; }
    bool CanOK { get; }
    ICommand OK { get; }
    ICommand Cancel { get; }
    bool? DialogResult { get; set; }

    LatinHypercubeDesign LatinHypercubeDesign { get; set; }
  }

  internal interface IRCParameterViewModel
  {
    string Name { get; }
    ICommand? SetKeyboardTarget { get; }
    double? CorrelationN { get; set; }
    string? CorrelationT { get; set; }
  }

  internal interface IRCConfigurationViewModel
  {
    bool IsMc2dInstalled { get; }

    Arr<string> ParameterNames { get; }

    IRCParameterViewModel[][]? RCParameterViewModels { get; }

    string? TargetParameterV { get; }
    string? TargetParameterH { get; }
    Arr<double> TargetCorrelations { get; }

    RankCorrelationDesignType RankCorrelationDesignType { get; set; }

    ICommand Disable { get; }
    bool CanOK { get; }
    ICommand OK { get; }
    ICommand Cancel { get; }
    bool? DialogResult { get; set; }

    Arr<(string Parameter, Arr<double> Correlations)> Correlations { get; set; }
  }

  internal interface IViewCorrelationViewModel
  {
    Arr<string> ParameterNames { get; }

    IRCParameterViewModel[][] RCParameterViewModels { get; }

    ICommand Close { get; }
    bool? DialogResult { get; set; }
  }

  internal interface ISamplesViewModel
  {
    bool IsReadOnly { get; set; }

    Arr<string> Distributions { get; }
    ICommand ShareParameters { get; }

    Arr<string> Invariants { get; }

    int? NSamples { get; set; }
    int? Seed { get; set; }

    LatinHypercubeDesignType LatinHypercubeDesignType { get; }
    ICommand ConfigureLHS { get; }
    bool CanConfigureLHS { get; }

    RankCorrelationDesignType RankCorrelationDesignType { get; }
    ICommand ConfigureRC { get; }
    bool CanConfigureRC { get; }

    ICommand GenerateSamples { get; }
    bool CanGenerateSamples { get; }

    DataView? Samples { get; }
    ICommand ViewCorrelation { get; }
    bool CanViewCorrelation { get; }

    string[][]? Statistics { get; }

    ObservableCollection<IParameterSamplingViewModel> ParameterSamplingViewModels { get; }
  }

  internal interface IDesignViewModel
  {
    bool IsSelected { get; set; }

    DateTime? CreatedOn { get; }
    ICommand CreateDesign { get; }
    bool CanCreateDesign { get; }

    ICommand UnloadDesign { get; }
    bool CanUnloadDesign { get; }

    double AcquireOutputsProgress { get; }
    int NOutputsAcquired { get; }
    int NOutputsToAcquire { get; }
    ICommand AcquireOutputs { get; }
    bool CanAcquireOutputs { get; }
    ICommand CancelAcquireOutputs { get; }
    bool CanCancelAcquireOutputs { get; }
  }

  internal interface IOutputsSelectedSampleViewModel
  {
    int SelectedSample { get; set; }
    string? SampleIdentifier { get; }
    Arr<string> ParameterValues { get; }
    ICommand ShareParameterValues { get; }
  }

  internal interface IOutputsFilterViewModel
  {
    string IndependentVariableName { get; }
    string IndependentVariableValue { get; }
    string? IndependentVariableUnit { get; }

    string OutputName { get; }
    string From { get; }
    string To { get; }
    string? OutputUnit { get; }

    bool IsEnabled { get; }

    ICommand ToggleEnable { get; }
    ICommand Delete { get; }
  }

  public interface IOutputsEvidenceViewModel
  {
    Arr<ISelectableItemViewModel> Observations { get; }
  }

  internal interface IOutputsFilteredSamplesViewModel
  {
    string IndependentVariableName { get; }
    int? IndependentVariableIndex { get; }
    double? IndependentVariableValue { get; }
    string? IndependentVariableUnit { get; }

    string? OutputName { get; }

    double? FromN { get; set; }
    string? FromT { get; set; }

    double? ToN { get; set; }
    string? ToT { get; set; }

    ICommand AddNewFilter { get; }

    ObservableCollection<IOutputsFilterViewModel> OutputsFilterViewModels { get; }

    bool IsUnion { get; set; }
    bool IsEnabled { get; set; }
  }

  internal interface IOutputsViewModel
  {
    bool IsVisible { get; set; }
    Arr<string> OutputNames { get; }
    int SelectedOutputName { get; set; }
    PlotModel Outputs { get; }
    PlotController PlotController { get; }
    ICommand ToggleSeriesType { get; }
    bool IsSeriesTypeLine { get; }
    ICommand ResetAxes { get; }
    IOutputsSelectedSampleViewModel OutputsSelectedSampleViewModel { get; }
    IOutputsFilteredSamplesViewModel OutputsFilteredSamplesViewModel { get; }
    IOutputsEvidenceViewModel OutputsEvidenceViewModel { get; }
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
    ICommand LoadSamplingDesign { get; }
    ICommand DeleteSamplingDesign { get; }
    ICommand FollowKeyboardInDesignDigests { get; }
    Option<(DateTime CreatedOn, DateTime SelectedOn)> TargetSamplingDesign { get; set; }
  }

  internal interface IViewModel
  {
    IParametersViewModel ParametersViewModel { get; }
    ISamplesViewModel SamplesViewModel { get; }
    IDesignViewModel DesignViewModel { get; }
    IOutputsViewModel OutputsViewModel { get; }
    IDesignDigestsViewModel DesignDigestsViewModel { get; }
  }
}
