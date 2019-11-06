using Ninject;

namespace RVisUI.Ioc.Design
{
  class NinjectBootstrapper : INinjectBootstrapper
  {
    public NinjectBootstrapper()
    {
      Kernel = new StandardKernel();
    }

    public void LoadModules()
    {
      ////Services
      //Kernel.Load(new ServiceModule());
      // ViewModels
      Kernel.Load(new ViewModelModule());

      Kernel.Load(new AppModule());
    }

    public IKernel Kernel { get; set; }
  }
}
