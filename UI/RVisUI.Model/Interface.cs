using LanguageExt;
using RVis.Base;
using RVis.Model;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RVisUI.Model
{
  public interface IAppSettings : INotifyPropertyChanged
  {
    bool RestoreWindow { get; set; }
    string PrimaryColorName { get; set; }
    int PrimaryColorHue { get; set; }
    string SecondaryColorName { get; set; }
    int SecondaryColorHue { get; set; }
    string PrimaryForegroundColorName { get; set; }
    int PrimaryForegroundColorHue { get; set; }
    string SecondaryForegroundColorName { get; set; }
    int SecondaryForegroundColorHue { get; set; }
    bool IsBaseDark { get; set; }
    string ModuleConfiguration { get; set; }
    int RThrottlingUseCores { get; set; }
    string PathToSimLibrary { get; set; }
    double Zoom { get; set; }
    T Get<T>(string name);
    void Set<T>(string name, T value);
  }

  public interface ISimSharedState : INotifyPropertyChanged
  {
    Arr<SimParameterSharedState> ParameterSharedStates { get; }
    IObservable<(Arr<SimParameterSharedState> ParameterSharedStates, ObservableQualifier ObservableQualifier)> ParameterSharedStateChanges { get; }
    void ShareParameterState(Arr<(string Name, double Value, double Minimum, double Maximum, Option<IDistribution> Distribution)> state);
    void UnshareParameterState(Arr<string> names);

    Arr<SimElementSharedState> ElementSharedStates { get; }
    IObservable<(Arr<SimElementSharedState> ElementSharedStates, ObservableQualifier ObservableQualifier)> ElementSharedStateChanges { get; }
    void ShareElementState(Arr<string> names);
    void UnshareElementState(Arr<string> names);

    Arr<SimObservationsSharedState> ObservationsSharedStates { get; }
    IObservable<(Arr<SimObservationsSharedState> ObservationsSharedStates, ObservableQualifier ObservableQualifier)> ObservationsSharedStateChanges { get; }
    void ShareObservationsState(Arr<string> references);
    void UnshareObservationsState(Arr<string> references);
  }

  public interface IAppState : INotifyPropertyChanged
  {
    string Status { get; set; }
    string MainWindowTitle { get; set; }
    void Initialize(string[] args);
    Option<Simulation> Target { get; set; }
    IObservable<Option<Simulation>> Simulation { get; }
    ISimData SimData { get; }
    ISimSharedState SimSharedState { get; }
    SimDataSessionLog SimDataSessionLog { get; }
    ISimEvidence SimEvidence { get; }
    void ResetSimDataService();
    Arr<(string ID, string DisplayName, string DisplayIcon, object View, object ViewModel)> UIComponents { get; set; }
    (string ID, string DisplayName, string DisplayIcon, object View, object ViewModel) ActiveUIComponent { get; set; }
    object ActiveViewModel { get; set; }
    Arr<ModuleInfo> LoadModules();
    string ExtraModulePath { get; set; }
    Arr<(string Name, string Value)> RVersion { get; set; }
    Arr<(string Package, string Version)> InstalledRPackages { get; set; }
    bool GetStartUpArg(StartUpOption startUpOption, out string arg);
  }

  public interface IReactiveSafeInvoke
  {
    bool React { get; }
    IDisposable SuspendedReactivity { get; }
    Action<T> SuspendAndInvoke<T>(Action<T> action, [CallerMemberName]string subject = "");
  }

  public interface IAppService
  {
    IObservable<long> SecondInterval { get; }
    bool CheckAccess();
    bool ShowDialog(object view, object viewModel, object parentViewModel);
    bool ShowDialog(object viewModel, object parentViewModel);
    bool AskUserYesNoQuestion(string question, string prompt, string about);
    bool BrowseForDirectory(string startPath, out string pathToDirectory);
    bool OpenFile(string purpose, string initialDirectory, string filter, out string pathToFile);
    bool SaveFile(string purpose, string initialDirectory, string filter, string extension, out string pathToFile);
    void Notify(string about, string subject, Exception ex, object originatingViewModel = default);
    void Notify(NotificationType type, string about, string subject, string detail, object originatingViewModel = default);
    void ScheduleAction(Action action);
    void ScheduleLowPriorityAction(Action action);
    IRVisServerPool RVisServerPool { get; }
    void ResetRVisServerPool();
    void Initialize();
    IReactiveSafeInvoke GetReactiveSafeInvoke();
    Action<T> SafeInvoke<T>(Action<T> action, [CallerMemberName]string subject = "");
  }

  public interface IRVisExtensibility
  {
    object GetView();
    object GetViewModel();
  }

  public interface ITaskRunnerContainer
  {
    Arr<ITaskRunner> GetTaskRunners();
  }

  public interface ITaskRunner
  {
    bool IsRunningTask { get; }
    string TaskName { get; }
    bool CanCancelTask { get; }
    void HandleCancelTask();
    IObservable<string> TaskMessages { get; }
  }

  public interface ISimSharedStateBuilder
  {
    SimSharedStateBuild BuildType { get; }
    void AddParameter(string name, double value, double minimum, double maximum, Option<IDistribution> distribution);
    void AddOutput(string name);
    void AddObservations(string reference);
  }

  public interface ISharedStateProvider
  {
    void ApplyState(
      SimSharedStateApply applyType,
      Arr<(SimParameter Parameter, double Minimum, double Maximum, Option<IDistribution> Distribution)> parameterSharedStates,
      Arr<SimElement> elementSharedStates,
      Arr<SimObservations> observationsSharedStates
      );
    void ShareState(ISimSharedStateBuilder sharedStateBuilder);
  }

  public interface ICommonConfiguration
  {
    bool? AutoApplyParameterSharedState { get; set; }
    bool? AutoShareParameterSharedState { get; set; }

    bool? AutoApplyElementSharedState { get; set; }
    bool? AutoShareElementSharedState { get; set; }

    bool? AutoApplyObservationsSharedState { get; set; }
    bool? AutoShareObservationsSharedState { get; set; }
  }

  public interface IExportedDataProvider
  {
    DataExportConfiguration GetDataExportConfiguration(
      string rootExportDirectory
      );

    void ExportData(
      DataExportConfiguration dataExportConfiguration
      );
  }
}
