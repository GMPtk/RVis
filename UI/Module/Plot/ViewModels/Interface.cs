using LanguageExt;
using OxyPlot;
using RVis.Data;
using RVis.Model;
using RVisUI.AppInf;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Plot
{
  public interface IDepVarConfigViewModel
  {
    ICommand ToggleView { get; }
    bool IsViewOpen { get; set; }

    ISelectableItemViewModel SelectedElement { get; set; }

    ObservableCollection<ISelectableItemViewModel> MRUElements { get; }
    ObservableCollection<ISelectableItemViewModel> LRUElements { get; }

    Arr<string> InsetOptions { get; }
    int SelectedInsetOption { get; set; }

    Arr<ISelectableItemViewModel> SupplementaryElements { get; set; }
    Arr<ISelectableItemViewModel> Observations { get; set; }

    bool IsScaleLogarithmic { get; set; }
  }

  public interface ITraceDataPlotViewModel
  {
    TraceDataPlotState State { get; }

    Arr<(string SeriesName, Arr<(string SerieName, NumDataTable Serie)> Series)> DataSet { get; set; }
    Arr<string> SeriesNames { get; set; }
    int SelectedIndexSeries { get; set; }

    IDepVarConfigViewModel DepVarConfigViewModel { get; }

    PlotModel PlotModel { get; set; }

    ICommand ToggleSeriesType { get; }
    bool IsSeriesTypeLine { get; set; }
    ICommand ToggleLockAxesOriginToZeroZero { get; }
    bool IsAxesOriginLockedToZeroZero { get; set; }
    ICommand ResetAxisRanges { get; }
    ICommand RemoveChart { get; }
  }

  public interface IViewModel
  {
    ITraceViewModel TraceViewModel { get; }
    IParametersViewModel ParametersViewModel { get; }
    IOutputsViewModel OutputsViewModel { get; }
  }

  public interface ITraceViewModel
  {
    Arr<(string SeriesName, Arr<(string SerieName, NumDataTable Serie)> Series)> DataSet { get; set; }
    Arr<ITraceDataPlotViewModel> TraceDataPlotViewModels { get; set; }
    int ChartGridLayout { get; set; }
    int IsWorkingSetPanelOpen { get; set; }
    ObservableCollection<IParameterViewModel> WorkingSet { get; }
    Stck<SimInput> SessionEdits { get; set; }
    ICommand UndoWorkingChange { get; }
    ICommand PlotWorkingChanges { get; }
    bool HasPendingWorkingChanges { get; set; }
    bool IsSelected { get; set; }
  }

  public interface IParameterViewModel
  {
    string Name { get; }
    string SortKey { get; }
    string DefaultValue { get; }
    string? Unit { get; }
    string? Description { get; }
    bool IsSelected { get; set; }
    ICommand ToggleSelect { get; }
    string? TValue { get; set; }
    ICommand ResetValue { get; }
    double? NValue { get; set; }
    double Minimum { get; set; }
    ICommand IncreaseMinimum { get; }
    bool CanIncreaseMinimum { get; set; }
    ICommand DecreaseMinimum { get; }
    double Maximum { get; set; }
    ICommand IncreaseMaximum { get; }
    ICommand DecreaseMaximum { get; }
    bool CanDecreaseMaximum { get; set; }
    string? Ticks { get; set; }
    void Set(double value, double minimum, double maximum);
  }

  public interface IParametersViewModel
  {
    ObservableCollection<IParameterViewModel> UnselectedParameters { get; }
    ObservableCollection<IParameterViewModel> SelectedParameters { get; }
  }

  public interface ILogEntryViewModel
  {
    SimDataLogEntry LogEntry { get; }
    string EnteredOn { get; }
    string RequesterTypeName { get; }
    string ParameterAssignments { get; }
  }

  public interface IOutputGroupViewModel
  {
    OutputGroup OutputGroup { get; }
    string CreatedOn { get; }
    string Name { get; }
    string Description { get; }
  }

  public interface IOutputsViewModel
  {
    ObservableCollection<ILogEntryViewModel> LogEntryViewModels { get; }
    ILogEntryViewModel[]? SelectedLogEntryViewModels { get; set; }
    ICommand LoadLogEntry { get; }
    ICommand CreateOutputGroup { get; }
    ICommand FollowKeyboardInLogEntries { get; }

    ObservableCollection<IOutputGroupViewModel> OutputGroupViewModels { get; }
    IOutputGroupViewModel? SelectedOutputGroupViewModel { get; set; }
    ICommand LoadOutputGroup { get; }
    ICommand FollowKeyboardInOutputGroups { get; }
  }
}
