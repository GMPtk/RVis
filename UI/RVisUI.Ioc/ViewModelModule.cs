using Ninject.Modules;
using RVisUI.Mvvm;

namespace RVisUI.Ioc
{
  class ViewModelModule : NinjectModule
  {
    public override void Load()
    {
      Bind<IHomeViewModel>().To<HomeViewModel>().InSingletonScope();
      Bind<ISimulationHomeViewModel>().To<SimulationHomeViewModel>().InSingletonScope();
      Bind<IZoomViewModel>().To<ZoomViewModel>().InSingletonScope();
    }
  }
}
