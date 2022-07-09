using Ninject;

namespace RVisUI.Ioc
{
  public class NinjectServiceLocator
  {
    public IKernel Kernel { get; private set; }

    public NinjectServiceLocator(IKernel kernel)
    {
      Kernel = kernel;
    }
  }
}
