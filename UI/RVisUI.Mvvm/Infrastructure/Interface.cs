using LanguageExt;
using RVis.Data;
using RVis.Model;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace RVisUI.Mvvm
{
  public interface IFailedStartUpViewModel
  {

  }

  public interface IHomeViewModel
  {
    ISelectSimulationViewModel SelectSimulationViewModel { get; }
    IImportSimulationViewModel ImportSimulationViewModel { get; }
    ILibraryViewModel LibraryViewModel { get; }
  }

  public interface ILibraryViewModel
  {
    string Location { get; set; }
    ICommand ChooseDirectory { get; }
  }

  public interface ISimulationViewModel
  {
    Simulation Simulation { get; }
    string Title { get; }
    string Description { get; }
    string DirectoryName { get; }
  }

  public interface ISelectSimulationViewModel
  {
    ObservableCollection<ISimulationViewModel> SimulationVMs { get; }
    ISimulationViewModel SelectedSimulationVM { get; set; }
    string PathToLibrary { get; set; }
    ICommand OpenSimulation { get; }
    ICommand DeleteSimulation { get; }
    string RVersion { get; set; }
  }

  public interface ISelectExecViewModel
  {
    Arr<string> UnaryFunctions { get; }
    int UnaryFunctionSelectedIndex { get; set; }
    Arr<string> ScalarSets { get; }
    int ScalarSetSelectedIndex { get; set; }
    ICommand OK { get; }
    ICommand Cancel { get; }
    bool? DialogResult { get; set; }
  }

  public interface IChangeDescriptionUnitViewModel
  {
    string TargetSymbol { get; set; }
    string Description { get; set; }
    string Unit { get; set; }
    object[][] LineSymDescUnit { get; }
    ICommand OK { get; }
    ICommand Cancel { get; }
    bool? DialogResult { get; set; }
  }

  public interface IParameterCandidateViewModel
  {
    bool IsUsed { get; set; }
    string Name { get; set; }
    double Value { get; set; }
    string Unit { get; set; }
    string Description { get; set; }
    ICommand ChangeUnitDescription { get; set; }
  }

  public interface IElementCandidateViewModel
  {
    bool IsUsed { get; set; }
    string Name { get; set; }
    string ValueName { get; set; }
    bool IsIndependentVariable { get; set; }
    string Values { get; set; }
    string Unit { get; set; }
    string Description { get; set; }
    ICommand ChangeUnitDescription { get; set; }
  }

  public interface IImportExecViewModel
  {
    string ExecInvocation { get; }
    Arr<IParameterCandidateViewModel> ParameterCandidates { get; }
    ICommand UseAllParameters { get; }
    ICommand UseNoParameters { get; }
    IElementCandidateViewModel IndependentVariable { get; }
    Arr<IElementCandidateViewModel> ElementCandidates { get; }
    ICommand UseAllOutputs { get; }
    ICommand UseNoOutputs { get; }
    string SimulationName { get; set; }
    string SimulationDescription { get; set; }
    ICommand OK { get; }
    ICommand Cancel { get; }
    bool? DialogResult { get; set; }
  }

  public interface IImportTmplViewModel
  {
    string FileName { get; }
    Arr<IParameterCandidateViewModel> ParameterCandidates { get; }
    ICommand UseAllParameters { get; }
    ICommand UseNoParameters { get; }
    IElementCandidateViewModel IndependentVariable { get; set; }
    ICommand SetIndependentVariable { get; }
    ObservableCollection<IElementCandidateViewModel> ElementCandidates { get; }
    IElementCandidateViewModel SelectedElementCandidate { get; set; }
    ICommand UseAllOutputs { get; }
    ICommand UseNoOutputs { get; }
    string SimulationName { get; set; }
    string SimulationDescription { get; set; }
    ICommand OK { get; }
    ICommand Cancel { get; }
    bool? DialogResult { get; set; }
  }

  public interface IImportSimulationViewModel
  {
    ICommand BrowseForRFile { get; }
    string PathToRFile { get; set; }
    ICommand InspectRFile { get; }
    ManagedImport ManagedImport { get; set; }
    Arr<ISymbolInfo> UnaryFuncs { get; set; }
    Arr<ISymbolInfo> Scalars { get; set; }
    Arr<ISymbolInfo> ScalarSets { get; set; }
    Arr<ISymbolInfo> DataSets { get; set; }

    ISymbolInfo ExecutiveFunction { get; set; }
    ISymbolInfo ExecutiveFormal { get; set; }
    NumDataTable ExecutiveOutput { get; set; }
    ICommand SelectExecutive { get; }

    ICommand ImportUsingExec { get; }
    ICommand ImportUsingTmpl { get; }

    bool IsBusy { get; set; }
    string BusyWith { get; set; }
    ObservableCollection<string> BusyMessages { get; }
    bool EnableBusyCancel { get; set; }
    ICommand BusyCancel { get; }
  }

  public interface ISharedStateViewModel
  {
    ICommand OpenView { get; }
    ICommand CloseView { get; }
    bool IsViewOpen { get; set; }

    object[][] SharedParameters { get; set; }
    ICommand ApplyParametersState { get; }
    ICommand ShareParametersState { get; }

    object[][] SharedOutputs { get; set; }
    ICommand ApplyOutputsState { get; }
    ICommand ShareOutputsState { get; }

    object[][] SharedObservations { get; set; }
    ICommand ApplyObservationsState { get; }
    ICommand ShareObservationsState { get; }

    ICommand ApplyState { get; }
    ICommand ShareState { get; }
  }

  public interface ISimulationHomeViewModel
  {
    string Name { get; set; }
    ICommand ChangeCommonConfiguration { get; }
    ICommand Export { get; }
    ICommand Close { get; }
    bool IsBusy { get; set; }
    string BusyWith { get; set; }
    ObservableCollection<string> BusyMessages { get; }
    bool EnableBusyCancel { get; set; }
    ICommand BusyCancel { get; }
    ISharedStateViewModel SharedStateViewModel { get; }
    int UIComponentIndex { get; set; }
    string ActiveUIComponentName { get; set; }
  }

  public interface ICommonConfigurationViewModel
  {
    Arr<ModuleViewModel> ModuleViewModels { get; }
    ICommand OK { get; }
    ICommand Cancel { get; }
    bool? DialogResult { get; set; }
  }

  public interface IModuleNotSupportedViewModel
  {
    string ModuleName { get; }
    Arr<string> MissingRPackageNames { get; }
  }

  public interface ISelectableOutputViewModel
  {
    public string Name { get; }
    public bool IsSelected { get; set; }
  }

  public interface IDataExportConfigurationViewModel
  {
    DataExportConfiguration DataExportConfiguration { get; set; }
    string Title { get; }
    string RootExportDirectory { get; set; }
    ICommand BrowseForRootExportDirectory { get; }
    string ExportDirectoryName { get; set; }
    bool OpenAfterExport { get; set; }
    Arr<ISelectableOutputViewModel> Outputs { get; }
    ICommand OK { get; }
    ICommand Cancel { get; }
    bool? DialogResult { get; set; }
  }
}
