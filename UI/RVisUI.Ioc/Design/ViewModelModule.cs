using Ninject.Modules;
using RVisUI.Mvvm;

namespace RVisUI.Ioc.Design
{
  class ViewModelModule : NinjectModule
  {
    public override void Load()
    {
      Bind<IHomeViewModel>().To<Mvvm.Design.HomeViewModel>().InSingletonScope();
      Bind<ISimulationHomeViewModel>().To<Mvvm.Design.SimulationHomeViewModel>().InSingletonScope();
      Bind<IZoomViewModel>().To<Mvvm.Design.ZoomViewModel>().InSingletonScope();
    }
  }
}
