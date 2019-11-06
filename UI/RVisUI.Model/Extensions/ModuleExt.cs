using LanguageExt;
using RVis.Model;
using RVis.Model.Extensions;

namespace RVisUI.Model.Extensions
{
  public static class ModuleExt
  {
    public static void SaveModuleData<T>(
      this Simulation simulation, 
      T data, 
      object instance, 
      string name
      ) =>
      simulation.SavePrivateData(
        data, 
        instance.GetType().Assembly.GetName().Name, 
        instance.GetType().Name, 
        name
        );

    public static Option<T> LoadModuleData<T>(
      this Simulation simulation,
      object instance,
      string name
      ) =>
      simulation.LoadPrivateData<T>(
        instance.GetType().Assembly.GetName().Name,
        instance.GetType().Name,
        name
        );
  }
}
