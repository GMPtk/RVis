using Ninject.Modules;
using RVisUI.Ioc.Mvvm;
using RVisUI.Model;

namespace RVisUI.Ioc
{
  internal class AppModule : NinjectModule
  {
    public override void Load()
    {
      Bind<IAppSettings>().To<AppSettings>().InSingletonScope();
      Bind<IAppService>().To<AppService>().InSingletonScope();
      Bind<IAppSettingsViewModel>().To<AppSettingsViewModel>().InSingletonScope();
    }
  }
}
