using LanguageExt;
using ReactiveUI;

namespace RVisUI.Mvvm
{
  public class ModuleNotSupportedViewModel : IModuleNotSupportedViewModel
  {
    public ModuleNotSupportedViewModel(string moduleName, Arr<string> missingRPackageNames)
    {
      ModuleName = moduleName;
      MissingRPackageNames = missingRPackageNames;
    }

    public string ModuleName { get; }

    public Arr<string> MissingRPackageNames { get; }
  }
}
