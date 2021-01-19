using Nett;
using RVisUI.Model;
using System.ComponentModel;

namespace Sampling
{
  [DisplayName("Sampling")]
  [DisplayIcon("ChartBellCurve")]
  [Description("Monte Carlo method applied to simulation parameters")]
  [Purpose(ModulePurpose.Uncertainty | ModulePurpose.Sampling)]
  [SupportedTasks("DistributionSampling", "FileSampling")]
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

    public IRunControlTask GetRunControlTask(string type, TomlTable taskSpec) => 
      SamplingTasks.Create(type, taskSpec, _appState, _appService);

    private readonly IAppState _appState;
    private readonly IAppService _appService;
    private readonly IAppSettings _appSettings;
  }
}
