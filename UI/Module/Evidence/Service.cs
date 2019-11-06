using RVisUI.Model;
using System.ComponentModel;

namespace Evidence
{
  [DisplayName("Evidence")]
  [DisplayIcon("ChartTimelineVariant")]
  [Description("Supporting data")]
  [Purpose(ModulePurpose.Administration | ModulePurpose.Output)]
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
