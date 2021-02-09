using LanguageExt;
using Ninject;
using ReactiveUI;
using RVisUI.Model;
using System;
using System.Reactive.Linq;
using System.Windows.Input;
using static LanguageExt.Prelude;
using static RVis.Base.Check;
using static RVisUI.Mvvm.Logger;

namespace RVisUI.Mvvm
{
  public sealed class AcatHostViewModel : ReactiveObject, IAcatHostViewModel
  {
    public AcatHostViewModel(
      IAppState appState,
      IAppService appService
      )
    {
      _appState = appState;
      _appService = appService;

      Import = ReactiveCommand.Create(HandleImport);
      Load = ReactiveCommand.Create(HandleLoad);

      try
      {
        var acatViewModels = appService.Factory.GetAll<IAcatViewModel>().ToArr();

        var pathToAcat = Acat.Configure();

        acatViewModels.Iter(vm => vm.Load(pathToAcat));

        AcatViewModels = acatViewModels.Filter(vm => vm.IsReady);

        _isVisible = !AcatViewModels.IsEmpty;

        if (_isVisible) SelectedAcatViewModel = AcatViewModels.Head();

        CanConfigure = SelectedAcatViewModel?.CanConfigureSimulation == true;
      }
      catch (Exception ex)
      {
        Log.Error(ex);
      }

      Observable
        .Merge(
          AcatViewModels.Map(avm => avm.ObservableForProperty(vm => vm.CanConfigureSimulation))
        )
        .Subscribe(
          _ => CanConfigure = SelectedAcatViewModel?.CanConfigureSimulation == true
          );

      this
        .ObservableForProperty(vm => vm.SelectedAcatViewModel)
        .Subscribe(_ => CanConfigure = SelectedAcatViewModel?.CanConfigureSimulation == true);
    }

    public bool IsVisible
    {
      get => _isVisible;
      set => this.RaiseAndSetIfChanged(ref _isVisible, value);
    }
    private bool _isVisible;

    public ICommand Import { get; }

    public ICommand Load { get; }

    public bool CanConfigure
    {
      get => _canConfigure;
      set => this.RaiseAndSetIfChanged(ref _canConfigure, value);
    }
    private bool _canConfigure;

    public Arr<IAcatViewModel> AcatViewModels { get; }

    public IAcatViewModel? SelectedAcatViewModel
    {
      get => _selectedAcatViewModel;
      set => this.RaiseAndSetIfChanged(ref _selectedAcatViewModel, value);
    }
    private IAcatViewModel? _selectedAcatViewModel;

    private void HandleImport() =>
      ConfigureSimulation(import: true);

    private void HandleLoad() => 
      ConfigureSimulation(import: false);

    private void ConfigureSimulation(bool import)
    {
      RequireNotNull(SelectedAcatViewModel);
      RequireTrue(SelectedAcatViewModel.CanConfigureSimulation);

      try
      {
        _appState.Target = SelectedAcatViewModel.ConfigureSimulation(import);
      }
      catch (Exception ex)
      {
        Log.Error(ex);
        _appService.Notify(nameof(AcatHostViewModel), nameof(HandleLoad), ex);
      }
    }

    private readonly IAppState _appState;
    private readonly IAppService _appService;
  }
}
