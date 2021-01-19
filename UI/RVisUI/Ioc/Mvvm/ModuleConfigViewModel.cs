using ReactiveUI;
using RVisUI.Model;

namespace RVisUI.Ioc.Mvvm
{
  public class ModuleConfigViewModel : ReactiveObject
  {
    public ModuleConfigViewModel()
    {

    }

    public ModuleConfigViewModel(ModuleInfo moduleInfo)
    {
      Name = moduleInfo.DisplayName;
      Description = moduleInfo.Description;
      IsEnabled = moduleInfo.IsEnabled;

      _moduleInfo = moduleInfo;
    }

    public string? Name
    {
      get => _name;
      set => this.RaiseAndSetIfChanged(ref _name, value);
    }
    private string? _name;

    public string? Description
    {
      get => _description;
      set => this.RaiseAndSetIfChanged(ref _description, value);
    }
    private string? _description;

    public bool IsEnabled
    {
      get => _isEnabled;
      set => this.RaiseAndSetIfChanged(ref _isEnabled, value);
    }
    private bool _isEnabled;

    public ModuleInfo? ModuleInfo
    {
      get => _moduleInfo;
      set => this.RaiseAndSetIfChanged(ref _moduleInfo, value);
    }
    private ModuleInfo? _moduleInfo;
  }
}
