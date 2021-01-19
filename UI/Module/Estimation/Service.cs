using Nett;
using RVisUI.Model;
using System;
using System.ComponentModel;

namespace Estimation
{
  [DisplayName("Parameter Estimation")]
  [DisplayIcon("ChartFinance")]
  [Description("Model fitting by numerical approximation")]
  [Purpose(ModulePurpose.Calibration | ModulePurpose.Estimation | ModulePurpose.Case)]
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
      throw new InvalidOperationException($"{nameof(Estimation)} does not support task: {type}");

    private readonly IAppState _appState;
    private readonly IAppService _appService;
    private readonly IAppSettings _appSettings;
  }
}
