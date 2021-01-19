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
    string? Distribution { get; set; }
    bool IsSelected { get; set; }
    ICommand ToggleSelect { get; }
    string SortKey { get; }
  }

  internal interface IParametersViewModel
  {
    bool IsVisible { get; }
    Arr<IParameterViewModel> AllParameterViewModels { get; }
    ObservableCollection<IParameterViewModel> SelectedParameterViewModels { get; }
    int SelectedParameterViewModel { get; set; }
    IParameterDistributionViewModel ParameterDistributionViewModel { get; }
  }

  internal interface IDesignViewModel
  {
    Arr<string> Factors { get; }
    Arr<string> Invariants { get; }

    SensitivityMethod SensitivityMethod { get; set; }
    int? NoOfRuns { get; set; }
    int? NoOfSamples { get; set; }

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

    DataView? Inputs { get; }
    int SelectedInputIndex { get; set; }
    ICommand ShareParameters { get; }
    bool CanShareParameters { get; }
    ICommand ViewError { get; }
    bool CanViewError { get; }
    bool ShowIssues { get; set; }
    bool HasIssues { get; }

    bool IsSelected { get; set; }
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

  internal interface IOutputViewModel
  {
    string Name { get; }
    bool IsSelected { get; set; }
  }

  internal interface IRankedParameterViewModel
  {
    string Name { get; }
    double Score { get; }
    bool IsSelected { get; set; }
  }

  internal interface IRankingViewModel
  {
    string? FromText { get; set; }
    double? From { get; }

    string? ToText { get; set; }
    double? To { get; }

    string? XUnits { get; }

    Arr<IOutputViewModel> OutputViewModels { get; }

    Arr<IRankedParameterViewModel> RankedParameterViewModels { get; }

    bool CanOK { get; }
    ICommand OK { get; }
    ICommand Cancel { get; }
    bool? DialogResult { get; set; }
  }

  internal interface IMorrisMeasuresViewModel
  {
    bool IsVisible { get; }
    bool IsReady { get; set; }
    bool IsSelected { get; set; }

    Arr<string> OutputNames { get; }
    int SelectedOutputName { get; set; }

    MorrisMeasureType MorrisMeasureType { get; set; }

    string? XUnits { get; }

    string? XBeginText { get; set; }
    double? XBegin { get; }

    string? XEndText { get; set; }
    double? XEnd { get; }

    Arr<IRankedParameterViewModel> RankedParameterViewModels { get; }
    Arr<string> RankedUsing { get; }
    double? RankedFrom { get; }
    double? RankedTo { get; }
    ICommand RankParameters { get; }
    ICommand UseRankedParameters { get; }
    ICommand ShareRankedParameters { get; }

    PlotModel PlotModel { get; }
  }

  internal interface IMuStarSigmaViewModel
  {
    PlotModel PlotModel { get; }
  }

  internal interface IMorrisEffectsViewModel
  {
    bool IsVisible { get; }
    bool IsReady { get; set; }

    IMuStarSigmaViewModel MuStarSigmaViewModel { get; }
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

    string? XUnits { get; }

    Arr<IRankedParameterViewModel> RankedParameterViewModels { get; }
    Arr<string> RankedUsing { get; }
    double? RankedFrom { get; }
    double? RankedTo { get; }
    ICommand UseRankedParameters { get; }
    ICommand ShareRankedParameters { get; }
  }

  internal interface IFast99MeasuresViewModel
  {
    bool IsVisible { get; }
    bool IsReady { get; set; }
    bool IsSelected { get; set; }

    Arr<string> OutputNames { get; }
    int SelectedOutputName { get; set; }

    Fast99MeasureType Fast99MeasureType { get; set; }

    string? XUnits { get; }

    string? XBeginText { get; set; }
    double? XBegin { get; }

    string? XEndText { get; set; }
    double? XEnd { get; }

    Arr<IRankedParameterViewModel> RankedParameterViewModels { get; }
    Arr<string> RankedUsing { get; }
    double? RankedFrom { get; }
    double? RankedTo { get; }
    ICommand RankParameters { get; }
    ICommand UseRankedParameters { get; }
    ICommand ShareRankedParameters { get; }

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

  internal interface IFast99EffectsViewModel
  {
    bool IsVisible { get; }
    bool IsReady { get; set; }

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

    string? XUnits { get; }

    Arr<IRankedParameterViewModel> RankedParameterViewModels { get; }
    Arr<string> RankedUsing { get; }
    double? RankedFrom { get; }
    double? RankedTo { get; }
    ICommand UseRankedParameters { get; }
    ICommand ShareRankedParameters { get; }
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
    ICommand LoadSensitivityDesign { get; }
    ICommand DeleteSensitivityDesign { get; }
    ICommand FollowKeyboardInDesignDigests { get; }
    Option<(DateTime CreatedOn, DateTime SelectedOn)> TargetSensitivityDesign { get; set; }
  }

  internal interface IViewModel
  {
    IParametersViewModel ParametersViewModel { get; }
    IDesignViewModel DesignViewModel { get; }
    IMorrisMeasuresViewModel MorrisMeasuresViewModel { get; }
    IMorrisEffectsViewModel MorrisEffectsViewModel { get; }
    IFast99MeasuresViewModel Fast99MeasuresViewModel { get; }
    IFast99EffectsViewModel Fast99EffectsViewModel { get; }
    IDesignDigestsViewModel DesignDigestsViewModel { get; }
  }
}
