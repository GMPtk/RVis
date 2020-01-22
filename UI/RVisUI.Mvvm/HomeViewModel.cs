using RVis.Base.Extensions;
using RVis.Model;
using RVisUI.Model;
using static System.IO.Directory;

namespace RVisUI.Mvvm
{
  public class HomeViewModel : IHomeViewModel
  {
    public HomeViewModel(
      IAppState appState,
      IAppService appService,
      IAppSettings appSettings
      )
    {
      var pathToSimLibrary = appSettings.PathToSimLibrary.ExpandPath();
      if (!Exists(pathToSimLibrary)) CreateDirectory(pathToSimLibrary);

      var simLibrary = new SimLibrary();

      SelectSimulationViewModel = new SelectSimulationViewModel(simLibrary, appState, appService);
      ImportSimulationViewModel = new ImportSimulationViewModel(simLibrary, appState, appService);
      ImportMCSimViewModel = new ImportMCSimViewModel(simLibrary, appState, appService);
      LibraryViewModel = new LibraryViewModel(simLibrary, appState, appService, appSettings);

      simLibrary.LoadFrom(pathToSimLibrary);
    }

    public ISelectSimulationViewModel SelectSimulationViewModel { get; }

    public IImportSimulationViewModel ImportSimulationViewModel { get; }
    public IImportMCSimViewModel ImportMCSimViewModel { get; }

    public ILibraryViewModel LibraryViewModel { get; }
  }
}
