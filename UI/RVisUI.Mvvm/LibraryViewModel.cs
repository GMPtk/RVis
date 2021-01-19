using ReactiveUI;
using RVis.Base.Extensions;
using RVis.Model;
using RVisUI.Model;
using System.Windows.Input;

namespace RVisUI.Mvvm
{
  public class LibraryViewModel : ReactiveObject, ILibraryViewModel
  {
    public LibraryViewModel(
      SimLibrary simLibrary, 
      IAppState appState,
      IAppService appService,
      IAppSettings appSettings 
      )
    {
      _simLibrary = simLibrary;
      _appState = appState;
      _appService = appService;
      _appSettings = appSettings;

      _location = _appSettings.PathToSimLibrary;
      ChooseDirectory = ReactiveCommand.Create(HandleChooseDirectory);
    }

    public string Location
    {
      get => _location;
      set => this.RaiseAndSetIfChanged(ref _location, value);
    }
    private string _location;

    public ICommand ChooseDirectory { get; }

    private void HandleChooseDirectory()
    {
      if (_appService.BrowseForDirectory(_appSettings.PathToSimLibrary.ExpandPath(), out string? pathToSimLibrary))
      {
        _simLibrary.LoadFrom(pathToSimLibrary);
        _appSettings.PathToSimLibrary = pathToSimLibrary.ContractPath();
        Location = _appSettings.PathToSimLibrary;
        _appState.Status = $"Location changed ({_simLibrary.Simulations.Count} simulation(s))";
      }
    }

    private readonly SimLibrary _simLibrary;
    private readonly IAppState _appState;
    private readonly IAppService _appService;
    private readonly IAppSettings _appSettings;
  }
}
