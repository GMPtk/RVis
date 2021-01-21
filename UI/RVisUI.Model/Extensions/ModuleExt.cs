using LanguageExt;
using RVis.Model;
using RVis.Model.Extensions;
using static RVis.Base.Check;

namespace RVisUI.Model.Extensions
{
  public static class ModuleExt
  {
    public static void SaveModuleData<T>(
      this Simulation simulation,
      T data,
      object instance,
      string name
      )
    {
      var type = instance.GetType();
      var assemblyName = type.Assembly.GetName();
      RequireNotNullEmptyWhiteSpace(assemblyName.Name);

      simulation.SavePrivateData(
        data,
        assemblyName.Name,
        type.Name,
        name
        );
    }

    public static Option<T> LoadModuleData<T>(
      this Simulation simulation,
      object instance,
      string name
      )
    {
      var type = instance.GetType();
      var assemblyName = type.Assembly.GetName();
      RequireNotNullEmptyWhiteSpace(assemblyName.Name);

      return simulation.LoadPrivateData<T>(
        assemblyName.Name,
        type.Name,
        name
        );
    }
  }
}
