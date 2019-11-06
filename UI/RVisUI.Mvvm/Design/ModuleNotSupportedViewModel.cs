using LanguageExt;
using static LanguageExt.Prelude;

namespace RVisUI.Mvvm.Design
{
  public class ModuleNotSupportedViewModel : IModuleNotSupportedViewModel
  {
    public string ModuleName => "My module";

    public Arr<string> MissingRPackageNames => Array("pkg1", "pkgwithalonglonglonglonglongname2");
  }
}
