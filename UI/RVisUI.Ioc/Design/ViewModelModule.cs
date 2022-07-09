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
      Bind<IHomeViewModel>().To<Mvvm.Design.HomeViewModel>().InSingletonScope();
      Bind<IImportRSimViewModel>().To<Mvvm.Design.ImportRSimViewModel>().InSingletonScope();
      Bind<IImportMCSimViewModel>().To<Mvvm.Design.ImportMCSimViewModel>().InSingletonScope();
      Bind<IModuleNotSupportedViewModel>().To<Mvvm.Design.ModuleNotSupportedViewModel>().InSingletonScope();
      Bind<IRunControlViewModel>().To<Mvvm.Design.RunControlViewModel>().InSingletonScope();
      Bind<ISelectSimulationViewModel>().To<Mvvm.Design.SelectSimulationViewModel>().InSingletonScope();
      Bind<ISimulationHomeViewModel>().To<Mvvm.Design.SimulationHomeViewModel>().InSingletonScope();
    }
  }
}
