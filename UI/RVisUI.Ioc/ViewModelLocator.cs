using CommonServiceLocator;
using RVisUI.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using static RVisUI.Wpf.WpfTools;

namespace RVisUI.Ioc
{
  public class ViewModelLocator
  {
    public ViewModelLocator()
    {
      NinjectBootstrapper = IsInDesignMode 
        ? new Design.NinjectBootstrapper() 
        : new NinjectBootstrapper();

      NinjectBootstrapper.LoadModules();

      _serviceLocator = new NinjectServiceLocator(NinjectBootstrapper.Kernel);
      ServiceLocator.SetLocatorProvider(() => _serviceLocator);

      SubscribeToServiceTypes(
        typeof(IHomeViewModel).Assembly, 
        $"{nameof(RVisUI)}.{nameof(Mvvm)}"
        );
    }

    public void SubscribeToServiceTypes(Assembly assembly, string viewModelNamespace)
    {
      var reNS = viewModelNamespace.Replace(".", @"\.");
      var reServiceTypeName = new Regex(reNS + @"\.I(.*)ViewModel");

      var interfaces = assembly.GetTypes().Where(t => t.IsInterface);
      foreach (var @interface in interfaces)
      {
        if (@interface.FullName is null) continue;

        var match = reServiceTypeName.Match(@interface.FullName);
        
        if (match.Success)
        {
          var serviceName = match.Groups[1].Value;
          _serviceLookUp.Add(serviceName, @interface);
        }
      }
    }

    public INinjectBootstrapper NinjectBootstrapper { get; }

    public object this[string serviceName] => 
      ServiceLocator.Current.GetInstance(_serviceLookUp[serviceName]);

    public static void Cleanup()
    {
      // TODO Clear the ViewModels
    }

    private readonly IServiceLocator _serviceLocator;
    private readonly IDictionary<string, Type> _serviceLookUp = new SortedDictionary<string, Type>();
  }
}
