using LanguageExt;
using ReactiveUI;
using RVisUI.Model;
using RVisUI.Model.Extensions;
using System;
using System.ComponentModel;
using System.Windows.Input;
using static RVis.Base.Check;

namespace RVisUI.Mvvm
{
  public class DataExportConfigurationViewModel : IDataExportConfigurationViewModel, INotifyPropertyChanged
  {
    public DataExportConfigurationViewModel(
      IAppService appService      
      )
    {
      _appService = appService;

      OK = ReactiveCommand.Create(HandleOK);
      Cancel = ReactiveCommand.Create(() => DialogResult = false);
      BrowseForRootExportDirectory = ReactiveCommand.Create(HandleBrowseForRootExportDirectory);

      _reactiveSafeInvoke = _appService.GetReactiveSafeInvoke();

      this
        .ObservableForProperty(vm => vm.DataExportConfiguration)
        .Subscribe(_reactiveSafeInvoke.SuspendAndInvoke<object>(ObserveDataExportConfiguration));
    }

    public DataExportConfiguration DataExportConfiguration
    {
      get => _dataExportConfiguration;
      set => this.RaiseAndSetIfChanged(ref _dataExportConfiguration, value, PropertyChanged);
    }
    private DataExportConfiguration _dataExportConfiguration;

    public string? Title 
    { 
      get => _title; 
      private set => this.RaiseAndSetIfChanged(ref _title, value, PropertyChanged); 
    }
    private string? _title;

    public string? RootExportDirectory
    {
      get => _rootExportDirectory;
      set => this.RaiseAndSetIfChanged(ref _rootExportDirectory, value, PropertyChanged);
    }
    private string? _rootExportDirectory;

    public ICommand BrowseForRootExportDirectory { get; }

    public string? ExportDirectoryName
    {
      get => _exportDirectoryName;
      set => this.RaiseAndSetIfChanged(ref _exportDirectoryName, value, PropertyChanged);
    }
    private string? _exportDirectoryName;

    public bool OpenAfterExport
    {
      get => _openAfterExport;
      set => this.RaiseAndSetIfChanged(ref _openAfterExport, value, PropertyChanged);
    }
    private bool _openAfterExport;

    public Arr<ISelectableOutputViewModel> Outputs 
    { 
      get => _outputs; 
      private set => this.RaiseAndSetIfChanged(ref _outputs, value, PropertyChanged); 
    }
    private Arr<ISelectableOutputViewModel> _outputs;

    public ICommand OK { get; }

    public ICommand Cancel { get; }

    public bool? DialogResult
    {
      get => _dialogResult;
      set => this.RaiseAndSetIfChanged(ref _dialogResult, value, PropertyChanged);
    }
    private bool? _dialogResult;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void HandleOK()
    {
      RequireNotNull(_title);
      RequireNotNull(_rootExportDirectory);
      RequireNotNull(_exportDirectoryName);

      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        DataExportConfiguration = new DataExportConfiguration(
          _title,
          _rootExportDirectory,
          _exportDirectoryName,
          _openAfterExport,
          _outputs.Map(vm => (vm.Name, vm.IsSelected))
          );
      }

      DialogResult = true;
    }

    private void HandleBrowseForRootExportDirectory()
    {
      var didBrowse = _appService.BrowseForDirectory(
        _rootExportDirectory,
        out string? pathToDirectory
        );

      if (didBrowse)
      {
        RootExportDirectory = pathToDirectory;
      }
    }

    private void ObserveDataExportConfiguration(object _)
    {
      Title = _dataExportConfiguration.Title;
      _rootExportDirectory = _dataExportConfiguration.RootExportDirectory;
      _exportDirectoryName = _dataExportConfiguration.ExportDirectoryName;
      _openAfterExport = _dataExportConfiguration.OpenAfterExport;
      Outputs = _dataExportConfiguration.Outputs
        .Map(o => new SelectableOutputViewModel(o.Name) { IsSelected = o.IncludeInExport })
        .ToArr<ISelectableOutputViewModel>();
    }

    private readonly IAppService _appService;
    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
  }
}
