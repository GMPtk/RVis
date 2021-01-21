using ReactiveUI;
using RVisUI.Model;

namespace RVisUI.Ioc.Mvvm
{
  public class ModuleConfigViewModel : ReactiveObject
  {
    public ModuleConfigViewModel(ModuleInfo moduleInfo)
    {
      Name = moduleInfo.DisplayName;
      Description = moduleInfo.Description;
      IsEnabled = moduleInfo.IsEnabled;
      ModuleInfo = moduleInfo;
    }

    public string? Name { get; }

    public string Description { get; }

    public bool IsEnabled
    {
      get => _isEnabled;
      set => this.RaiseAndSetIfChanged(ref _isEnabled, value);
    }
    private bool _isEnabled;

    public ModuleInfo ModuleInfo { get; }
  }
}
