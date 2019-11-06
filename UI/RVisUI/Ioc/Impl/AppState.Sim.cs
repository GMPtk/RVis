using LanguageExt;
using RVis.Model;
using RVisUI.Model;
using RVisUI.Mvvm;
using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using static LanguageExt.Prelude;

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

    public ISimData SimData => _simData;

    public ISimSharedState SimSharedState => _simSharedState;

    public SimDataSessionLog SimDataSessionLog => _simDataSessionLog;

    public ISimEvidence SimEvidence => _simEvidence;

    public void ResetSimDataService() => _simData.ResetService();

    private void ObserveSimulation(Option<Simulation> observed)
    {
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

    private SimData _simData;
    private SimSharedState _simSharedState;
    private SimDataSessionLog _simDataSessionLog;
    private SimEvidence _simEvidence;
  }
}
