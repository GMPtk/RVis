using Ninject.Modules;
using RVisUI.Mvvm;

namespace RVisUI.Ioc.Design
{
  class ViewModelModule : NinjectModule
  {
    public override void Load()
    {
      // /Impl/Acat
      Bind<IAcatViewModel>().To<Mvvm.Design.DrugXSimpleAcatViewModel>().InSingletonScope();
      Bind<IAcatViewModel>().To<Mvvm.Design.DrugXNotSoSimpleAcatViewModel>().InSingletonScope();

      // /Impl
      Bind<IZoomViewModel>().To<Mvvm.Design.ZoomViewModel>().InSingletonScope();

      // /
      Bind<IAcatHostViewModel>().To<Mvvm.Design.AcatHostViewModel>().InSingletonScope();
      //Bind<IFailedStartUpViewModel>().To<Mvvm.Design.FailedStartUpViewModel>().InSingletonScope();
      Bind<IHomeViewModel>().To<Mvvm.Design.HomeViewModel>().InSingletonScope();
      Bind<IImportMCSimViewModel>().To<Mvvm.Design.ImportMCSimViewModel>().InSingletonScope();
      Bind<IImportSimulationViewModel>().To<Mvvm.Design.ImportSimulationViewModel>().InSingletonScope();
      //Bind<ILibraryViewModel>().To<Mvvm.Design.LibraryViewModel>().InSingletonScope();
      Bind<IModuleNotSupportedViewModel>().To<Mvvm.Design.ModuleNotSupportedViewModel>().InSingletonScope();
      Bind<IRunControlViewModel>().To<Mvvm.Design.RunControlViewModel>().InSingletonScope();
      Bind<ISelectSimulationViewModel>().To<Mvvm.Design.SelectSimulationViewModel>().InSingletonScope();
      Bind<ISimulationHomeViewModel>().To<Mvvm.Design.SimulationHomeViewModel>().InSingletonScope();
    }
  }
}
