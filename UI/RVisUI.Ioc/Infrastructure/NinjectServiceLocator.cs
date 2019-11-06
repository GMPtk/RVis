using CommonServiceLocator;
using Ninject;
using System;
using System.Collections.Generic;

namespace RVisUI.Ioc
{
  public class NinjectServiceLocator : ServiceLocatorImplBase
  {
    public IKernel Kernel { get; private set; }

    public NinjectServiceLocator(IKernel kernel)
    {
      Kernel = kernel;
    }

    protected override object DoGetInstance(Type serviceType, string key)
    {
      // key == null must be specifically handled as not asking for a specific keyed instance
      // http://commonservicelocator.codeplex.com/wikipage?title=API%20Reference&referringTitle=Home
      //     The implementation should be designed to expect a null for the string key parameter, 
      //     and MUST interpret this as a request to get the "default" instance for the requested 
      //     type. This meaning of default varies from locator to locator.
      if (key == null)
      {
        return Kernel.Get(serviceType);
      }
      return Kernel.Get(serviceType, key);
    }

    protected override IEnumerable<object> DoGetAllInstances(Type serviceType)
    {
      return Kernel.GetAll(serviceType);
    }
  }
}
