using RVisUI.Model;
using System.ComponentModel;

namespace Sampling
{
  [DisplayName("Sampling")]
  [DisplayIcon("ChartBellCurve")]
  [Description("Monte Carlo method applied to simulation parameters")]
  [Purpose(ModulePurpose.Uncertainty | ModulePurpose.Sampling)]
  public sealed class Service : IRVisExtensibility
  {
    public Service(IAppState appState, IAppService appService, IAppSettings appSettings)
    {
      _appState = appState;
      _appService = appService;
      _appSettings = appSettings;
    }

    public object GetView() => 
      new View();

    public object GetViewModel() => 
      new ViewModel(_appState, _appService, _appSettings);

    private readonly IAppState _appState;
    private readonly IAppService _appService;
    private readonly IAppSettings _appSettings;
  }
}
