using Ninject;
using ReactiveUI;
using RVisUI.Model;

namespace RVisUI.Mvvm
{
  public class HomeViewModel : ReactiveObject, IHomeViewModel
  {
    public HomeViewModel(IAppService appService)
    {
      SelectSimulationViewModel = appService.Factory.Get<ISelectSimulationViewModel>();
      ImportSimulationViewModel = appService.Factory.Get<IImportSimulationViewModel>();
      ImportMCSimViewModel = appService.Factory.Get<IImportMCSimViewModel>();
      LibraryViewModel = appService.Factory.Get<ILibraryViewModel>();
      RunControlViewModel = appService.Factory.Get<IRunControlViewModel>();
      AcatHostViewModel = appService.Factory.Get<IAcatHostViewModel>();
    }

    public ISelectSimulationViewModel SelectSimulationViewModel { get; }
    public IImportSimulationViewModel ImportSimulationViewModel { get; }
    public IImportMCSimViewModel ImportMCSimViewModel { get; }
    public ILibraryViewModel LibraryViewModel { get; }
    public IRunControlViewModel RunControlViewModel { get; }
    public IAcatHostViewModel AcatHostViewModel { get; }

    public int SelectedIndex 
    { 
      get => _selectedIndex; 
      set => this.RaiseAndSetIfChanged(ref _selectedIndex, value); 
    }
    private int _selectedIndex;
  }
}
