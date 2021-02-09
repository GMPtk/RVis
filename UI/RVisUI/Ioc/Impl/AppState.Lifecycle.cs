using LanguageExt;
using ReactiveUI;
using RVis.Base;
using RVis.Base.Extensions;
using RVis.Model;
using RVisUI.Model;
using RVisUI.Mvvm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static LanguageExt.Prelude;
using static RVis.Base.Check;
using static RVisUI.Model.ModuleInfo;
using static System.Threading.Tasks.Task;

namespace RVisUI.Ioc
{
  public partial class AppState
  {
    public bool GetStartUpArg(StartUpOption startUpOption, out string? arg)
    {
      RequireNotNull(_startUpArgs);

      var pair = _startUpArgs.SingleOrDefault(t => t.Option == startUpOption);
      arg = pair.Value;
      return pair.Option != StartUpOption.None;
    }

    public void Dispose() => Dispose(true);

    private void Dispose(bool disposing)
    {
      if (!_disposed)
      {
        if (disposing)
        {
          DisposeUIComponents();
          _runControl?.Dispose();
          _simSharedState?.Dispose();
          _simEvidence?.Dispose();
          _simDataSessionLog?.Dispose();
          _simData?.Dispose();
          _targetSimulation.Dispose();
          _moduleConfigurationSubscription?.Dispose();
          _secondIntervalSubscription?.Dispose();
        }

        _disposed = true;
      }
    }

    private void ProcessStartUpArgs(string[] args)
    {
      var cliOptSpecs = Seq(

        // commands
        $"{StartUpOption.LoadSim}", $"{StartUpOption.l}",

        // arguments
        $"{StartUpOption.Name}=", $"{StartUpOption.n}=",
        $"{StartUpOption.ExtraModulePath}=", $"{StartUpOption.e}="

      ).ToCliOptSpecs();

      var startUpArgs = new List<(StartUpOption Option, string? Value)>();
      Option<string> extraModulePath = None;

      try
      {
        var cliOpts = args.ToCliOpts(cliOptSpecs);

        var option = cliOpts.GetOpt(StartUpOption.l, StartUpOption.LoadSim);
        var loadSim = option.IsSome;

        option = cliOpts.GetOpt(StartUpOption.n, StartUpOption.Name);
        var name = from value in option
                   from argument in value.Argument
                   select argument;

        if (loadSim)
        {
          var loadSimName = name.AssertSome(
            $"option {nameof(StartUpOption.LoadSim)} requires a {nameof(StartUpOption.Name)} argument"
            );
          startUpArgs.Add((StartUpOption.LoadSim, default));
          startUpArgs.Add((StartUpOption.Name, loadSimName));
        }

        option = cliOpts.GetOpt(StartUpOption.e, StartUpOption.ExtraModulePath);
        extraModulePath = from value in option
                          from argument in value.Argument
                          select argument;
      }
      catch (Exception ex)
      {
        _appService.Notify(
          nameof(AppState),
          nameof(StartUpOption),
          ex
          );
        App.Current.Log.Error(ex);
        App.Current.Shutdown(1);
      }

      _startUpArgs = startUpArgs.ToArray();

      ifSome(extraModulePath, emp => ExtraModulePath = emp);
    }

    private Task CurtailOperationsAsync()
    {
      ActiveViewModel = new FailedStartUpViewModel();
      return CompletedTask;
    }

    private async Task StartOperationsAsync(ServerLicense serverLicense)
    {
      _simData = new SimData(_appService.RVisServerPool);
      _simDataSessionLog = new SimDataSessionLog(_simData, _appService.SecondInterval);
      _simEvidence = new SimEvidence();
      _simSharedState = new SimSharedState();

      try
      {
        var pathToSimLibrary = _appSettings.PathToSimLibrary.ExpandPath();
        if (!Directory.Exists(pathToSimLibrary)) Directory.CreateDirectory(pathToSimLibrary);

        var simLibrary = new SimLibrary();
        await Run(() => simLibrary.LoadFrom(pathToSimLibrary));
        App.Current.NinjectKernel.Bind<SimLibrary>().ToConstant(simLibrary);
      }
      catch (Exception ex)
      {
        App.Current.Log.Error(ex);
        var _ = CurtailOperationsAsync();
        return;
      }

      try
      {
        var rClient = await serverLicense.GetRClientAsync();
        RVersion = await rClient.GetRversionAsync();
        InstalledRPackages = await rClient.GetInstalledPackagesAsync();
      }
      catch (Exception ex)
      {
        App.Current.Log.Error(ex);
        var _ = CurtailOperationsAsync();
        return;
      }
      finally
      {
        serverLicense.Dispose();
      }

      _moduleConfigurationSubscription = _appSettings
        .ObservableForProperty(s => s.ModuleConfiguration)
        .Subscribe(ObserveModuleConfiguration);

      _secondIntervalSubscription = _appService.SecondInterval.Subscribe(ObserveSecondInterval);

      Simulation.Subscribe(
        App.Current.AppService.SafeInvoke<Option<Simulation>>(ObserveSimulation)
        );

      ExecuteStartUpOptions();

      _appService.ScheduleLowPriorityAction(StartBackgroundTasks);
    }

    private void StartBackgroundTasks()
    {
      var jobSpecs = RunControlSpec.LoadJobSpecs(_appSettings);

      if (jobSpecs.IsEmpty) return;

      var services = GetServices(rebind: false);
      var moduleInfos = GetModuleInfos(services);
      var runControl = RunControlSpec.ToRunControl(jobSpecs, moduleInfos, _appSettings);

      if (runControl is null) return;

      RunControl = runControl;

      runControl.StartJobs();
    }

    private void ExecuteStartUpOptions()
    {
      if (GetStartUpArg(StartUpOption.LoadSim, out string _))
      {
        GetStartUpArg(StartUpOption.Name, out string? directoryName);

        var homeViewModel = RequireInstanceOf<IHomeViewModel>(App.Current.NinjectKernel.GetService(typeof(IHomeViewModel)));
        var selectSimulationViewModel = homeViewModel.SelectSimulationViewModel;

        var simulationVM = selectSimulationViewModel.SimulationVMs.SingleOrDefault(vm => vm.DirectoryName == directoryName);
        if (simulationVM == default)
        {
          _appService.Notify(
            NotificationType.Error,
            nameof(ExecuteStartUpOptions),
            nameof(StartUpOption.LoadSim),
            $"Unknown simulation directory in current library: {directoryName}"
            );
        }
        else
        {
          Target = Some(simulationVM.Simulation);
        }
      }
    }

    private (StartUpOption Option, string? Value)[]? _startUpArgs;
    private IDisposable? _moduleConfigurationSubscription;
    private IDisposable? _secondIntervalSubscription;
    private bool _disposed = false;
  }
}
