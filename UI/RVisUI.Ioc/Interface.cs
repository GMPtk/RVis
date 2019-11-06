using Ninject;

namespace RVisUI.Ioc
{
  public interface INinjectBootstrapper
  {
    IKernel Kernel { get; }
    void LoadModules();
  }
}
