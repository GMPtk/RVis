using LanguageExt;
using ReactiveUI;
using RVis.Base;
using RVis.Base.Extensions;
using RVisUI.Model;
using RVisUI.Model.Extensions;
using System;
using System.ComponentModel;

namespace RVisUI.Ioc
{
  public sealed partial class AppState : IAppState, IDisposable
  {
    public AppState(IAppSettings appSettings, IAppService appService)
    {
      _appSettings = appSettings;
      _appService = appService;

      _assemblyTitle = Meta.Product;
      MainWindowTitle = _assemblyTitle;
    }

    internal void Initialize(string[] args)
    {
      ProcessStartUpArgs(args);

      _appService.RVisServerPool.RequestServer().Match(
        StartOperationsAsync,
        CurtailOperationsAsync
        );
    }

    public string? Status
    {
      get => _status;
      set
      {
        if (value.IsAString()) _secondsStatusDisplayed = SECS_STATUS_DISPLAY_INTERVAL;
        this.RaiseAndSetIfChanged(ref _status, value ?? STANDBY_STATUS, PropertyChanged);
      }
    }
    private string? _status = STANDBY_STATUS;

    public string? MainWindowTitle
    {
      get => _mainWindowTitle;
      set => this.RaiseAndSetIfChanged(ref _mainWindowTitle, value, PropertyChanged);
    }
    private string? _mainWindowTitle;

    public Arr<(string Name, string Value)> RVersion
    {
      get => _rVersion;
      set => this.RaiseAndSetIfChanged(ref _rVersion, value, PropertyChanged);
    }
    private Arr<(string Name, string Value)> _rVersion;

    public Arr<(string Package, string Version)> InstalledRPackages
    {
      get => _installedRPackages;
      set => this.RaiseAndSetIfChanged(ref _installedRPackages, value, PropertyChanged);
    }
    private Arr<(string Package, string Version)> _installedRPackages;

    public RunControl? RunControl 
    { 
      get => _runControl;
      set => this.RaiseAndSetIfChanged(ref _runControl, value, PropertyChanged);
    }
    private RunControl? _runControl;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void ObserveSecondInterval(long _)
    {
      if (_secondsStatusDisplayed > 0)
      {
        if (--_secondsStatusDisplayed == 0) Status = default;
      }
    }

    private const int SECS_STATUS_DISPLAY_INTERVAL = 7;
    private const string STANDBY_STATUS = "Ready";

    private readonly IAppSettings _appSettings;
    private readonly IAppService _appService;
    private readonly string _assemblyTitle;
    private int _secondsStatusDisplayed;
  }
}
