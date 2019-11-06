using Ninject;

namespace RVisUI.Ioc
{
  class NinjectBootstrapper : INinjectBootstrapper
  {
    public NinjectBootstrapper()
    {
      Kernel = new StandardKernel(new NinjectSettings
      {
        AllowNullInjection = true
      });
    }

    public void LoadModules()
    {
      // Services
      Kernel.Load(new ServiceModule());
      // ViewModels
      Kernel.Load(new ViewModelModule());
    }

    public IKernel Kernel { get; private set; }
  }
}
