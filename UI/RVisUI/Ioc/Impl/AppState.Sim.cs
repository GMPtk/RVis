using LanguageExt;
using RVis.Base.Extensions;
using RVis.Model;
using RVisUI.Model;
using RVisUI.Mvvm;
using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using static LanguageExt.Prelude;
using static RVis.Base.Check;

namespace RVisUI.Ioc
{
  public partial class AppState
  {
    public Option<Simulation> Target
    {
      get => _targetSimulation.Value;
      set => _targetSimulation.OnNext(value);
    }

    public IObservable<Option<Simulation>> Simulation =>
      _targetSimulation.AsObservable();
    private readonly BehaviorSubject<Option<Simulation>> _targetSimulation =
      new BehaviorSubject<Option<Simulation>>(None);

    public ISimData SimData => 
      _simData ?? throw new InvalidOperationException("Trying to access state after failed start-up");

    public ISimSharedState SimSharedState => 
      _simSharedState ?? throw new InvalidOperationException("Trying to access state after failed start-up");

    public SimDataSessionLog SimDataSessionLog => 
      _simDataSessionLog ?? throw new InvalidOperationException("Trying to access state after failed start-up");

    public ISimEvidence SimEvidence => 
      _simEvidence ?? throw new InvalidOperationException("Trying to access state after failed start-up");

    public void ResetSimDataService() => _simData?.ResetService();

    private void ObserveSimulation(Option<Simulation> observed)
    {
      RequireNotNull(_simDataSessionLog);
      RequireNotNull(_simEvidence);
      RequireNotNull(_simSharedState);

      DisposeUIComponents();

      Type WithSome(Simulation simulation)
      {
        _simDataSessionLog.StartSession(simulation);
        _simEvidence.Load(simulation);
        _simSharedState.StartSharing(simulation);

        return typeof(ISimulationHomeViewModel);
      }

      Type WithNone()
      {
        _simSharedState.StopSharing();
        _simEvidence.Unload();
        _simDataSessionLog.EndSession();

        return typeof(IHomeViewModel);
      }

      try
      {
        var type = observed.Match(WithSome, WithNone);

        var viewModel = App.Current.NinjectKernel.GetService(type);

        CreateUIComponents(viewModel);

        ActiveViewModel = viewModel;
      }
      catch (Exception ex)
      {
        App.Current.AppService.Notify(
          NotificationType.Error,
          nameof(AppState),
          nameof(ObserveSimulation),
          ex.Message
          );
        App.Current.Log.Error(ex);
        Target = None;
      }
    }

    private SimData? _simData;
    private SimSharedState? _simSharedState;
    private SimDataSessionLog? _simDataSessionLog;
    private SimEvidence? _simEvidence;
  }
}
