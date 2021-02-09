using Ninject.Modules;
using RVisUI.Mvvm;

namespace RVisUI.Ioc
{
  class ViewModelModule : NinjectModule
  {
    public override void Load()
    {
      // /Impl/Acat
      Bind<IAcatViewModel>().To<DrugXSimpleAcatViewModel>().InSingletonScope();
      Bind<IAcatViewModel>().To<DrugXNotSoSimpleAcatViewModel>().InSingletonScope();

      // /Impl
      Bind<IZoomViewModel>().To<ZoomViewModel>().InSingletonScope();

      // /
      Bind<IAcatHostViewModel>().To<AcatHostViewModel>().InSingletonScope();
      Bind<IFailedStartUpViewModel>().To<FailedStartUpViewModel>().InSingletonScope();
      Bind<IHomeViewModel>().To<HomeViewModel>().InSingletonScope();
      Bind<IImportMCSimViewModel>().To<ImportMCSimViewModel>().InSingletonScope();
      Bind<IImportSimulationViewModel>().To<ImportSimulationViewModel>().InSingletonScope();
      Bind<ILibraryViewModel>().To<LibraryViewModel>().InSingletonScope();
      Bind<IModuleNotSupportedViewModel>().To<ModuleNotSupportedViewModel>().InSingletonScope();
      Bind<IRunControlViewModel>().To<RunControlViewModel>().InSingletonScope();
      Bind<ISelectSimulationViewModel>().To<SelectSimulationViewModel>().InSingletonScope();
      Bind<ISimulationHomeViewModel>().To<SimulationHomeViewModel>().InSingletonScope();
    }
  }
}
