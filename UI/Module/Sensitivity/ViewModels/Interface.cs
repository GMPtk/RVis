using LanguageExt;
using OxyPlot;
using RVisUI.AppInf;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Windows.Input;

namespace Sensitivity
{
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
    int SelectedParameterViewModel { get; set; }
    IParameterDistributionViewModel ParameterDistributionViewModel { get; }
  }

  internal interface IDesignViewModel
  {
    Arr<string> Factors { get; }
    Arr<string> Invariants { get; }

    int? SampleSize { get; set; }
    ICommand CreateDesign { get; }
    bool CanCreateDesign { get; }

    DateTime? DesignCreatedOn { get; }
    ICommand UnloadDesign { get; }
    bool CanUnloadDesign { get; }

    double AcquireOutputsProgress { get; }
    int NOutputsAcquired { get; }
    int NOutputsToAcquire { get; }
    ICommand AcquireOutputs { get; }
    bool CanAcquireOutputs { get; }
    ICommand CancelAcquireOutputs { get; }
    bool CanCancelAcquireOutputs { get; }

    DataView Inputs { get; }
    int SelectedInputIndex { get; set; }
    ICommand ShareParameters { get; }
    bool CanShareParameters { get; }
    ICommand ViewError { get; }
    bool CanViewError { get; }
    bool ShowIssues { get; set; }
    bool HasIssues { get; }

    bool IsSelected { get; set; }
  }

  internal interface IVarianceViewModel
  {
    bool IsVisible { get; }
    bool IsSelected { get; set; }

    Arr<string> OutputNames { get; }
    int SelectedOutputName { get; set; }

    VarianceMeasureType VarianceMeasureType { get; set; }

    PlotModel PlotModel { get; }
  }

  internal interface ILowryViewModel
  {
    PlotModel PlotModel { get; }
    ICommand UpdateSize { get; }
    int Width { get; }
    int Height { get; }
    ICommand ResetAxes { get; }
    ICommand ShowOptions { get; }
    ICommand ExportImage { get; }
  }

  internal interface ITraceViewModel
  {
    PlotModel PlotModel { get; }
    Arr<double> XValues { get; }
    Arr<double> YValues { get; }
    double SelectedX { get; set; }
    ICommand ResetAxes { get; }
    ICommand ShowOptions { get; }
    double ViewHeight { get; set; }
  }

  internal interface IEffectsViewModel
  {
    bool IsVisible { get; }

    ILowryViewModel LowryViewModel { get; }
    ITraceViewModel TraceViewModel { get; }

    Arr<string> OutputNames { get; }
    bool CanSelectOutputName { get; }
    int SelectedOutputName { get; set; }

    int PlaySpeed { get; }
    ICommand PlaySimulation { get; }
    bool CanPlaySimulation { get; }
    ICommand StopSimulation { get; }
    bool CanStopSimulation { get; }
    ICommand PlaySlower { get; }
    bool CanPlaySlower { get; }
    ICommand PlayFaster { get; }
    bool CanPlayFaster { get; }
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
    ICommand LoadSensitivityDesign { get; }
    ICommand DeleteSensitivityDesign { get; }
    ICommand FollowKeyboardInDesignDigests { get; }
    Option<(DateTime CreatedOn, DateTime SelectedOn)> TargetSensitivityDesign { get; set; }
  }

  internal interface IViewModel
  {
    IParametersViewModel ParametersViewModel { get; }
    IDesignViewModel DesignViewModel { get; }
    IVarianceViewModel VarianceViewModel { get; }
    IEffectsViewModel EffectsViewModel { get; }
    IDesignDigestsViewModel DesignDigestsViewModel { get; }
  }
}
