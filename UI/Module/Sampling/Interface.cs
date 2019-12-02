using LanguageExt;
using OxyPlot;
using RVis.Data;
using RVis.Model;
using RVisUI.AppInf;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Sampling
{
  internal interface IParameterSamplingViewModel
  {
    SimParameter Parameter { get; }
    string SortKey { get; }
    IDistribution Distribution { get; set; }
    Arr<double> Samples { get; set; }
    PlotModel Histogram { get; }
  }

  internal interface IParameterViewModel
  {
    string Name { get; }
    string Distribution { get; set; }
    bool IsSelected { get; set; }
    ICommand ToggleSelect { get; }
    string SortKey { get; }
  }

  internal interface IParametersViewModel
  {
    Arr<IParameterViewModel> AllParameterViewModels { get; }
    ObservableCollection<IParameterViewModel> SelectedParameterViewModels { get; }
    LatinHypercubeDesignType LatinHypercubeDesignType { get; }
    bool CanConfigureLHS { get; }
    ICommand ConfigureLHS { get; }
    int SelectedParameterViewModel { get; set; }
    IParameterDistributionViewModel ParameterDistributionViewModel { get; }
  }

  internal interface ILHSParameterViewModel
  {
    string Name { get; }
    double? Lower { get; set; }
    double? Upper { get; set; }
  }

  internal interface ILHSConfigurationViewModel
  {
    bool IsDiceDesignInstalled { get; }

    Arr<ILHSParameterViewModel> LHSParameterViewModels { get; }

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

    Arr<(string Parameter, double Lower, double Upper)> Variables { get; set; }
    LatinHypercubeDesign LatinHypercubeDesign { get; set; }
  }

  internal interface IDesignViewModel
  {
    int? NSamples { get; set; }
    int? Seed { get; set; }
    ICommand GenerateSamples { get; }
    bool CanGenerateSamples { get; set; }

    IDesignActivityViewModel ActivityViewModel { get; set; }

    DateTime? CreatedOn { get; set; }
    string Invariants { get; set; }
    ObservableCollection<IParameterSamplingViewModel> ParameterSamplingViewModels { get; }

    ICommand CreateDesign { get; }
    bool CanCreateDesign { get; set; }

    ICommand AcquireOutputs { get; }
    bool CanAcquireOutputs { get; set; }
    TimeSpan? EstimatedAcquireDuration { get; set; }
    Arr<(int Index, NumDataTable Output)> Outputs { get; set; }
    ICommand CancelAcquireOutputs { get; }
    bool CanCancelAcquireOutputs { get; set; }

    double Progress { get; set; }

    bool IsSelected { get; set; }
  }

  internal interface IDesignDigestViewModel
  {
    DateTime CreatedOn { get; }
    string Description { get; }
  }

  internal interface IDesignDigestsViewModel
  {
    ObservableCollection<IDesignDigestViewModel> DesignDigestViewModels { get; }
    IDesignDigestViewModel SelectedDesignDigestViewModel { get; set; }
    ICommand LoadSamplingDesign { get; }
    ICommand DeleteSamplingDesign { get; }
    ICommand FollowKeyboardInDesignDigests { get; }
    DateTime? TargetSamplingDesignCreatedOn { get; set; }
  }

  internal interface IViewModel
  {
    IParametersViewModel ParametersViewModel { get; }
    IDesignViewModel DesignViewModel { get; }
    IDesignDigestsViewModel DesignDigestsViewModel { get; }
  }
}
