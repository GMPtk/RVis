using LanguageExt;
using ReactiveUI;
using RVisUI.Model;
using System.Windows.Input;
using static LanguageExt.Prelude;

namespace RVisUI.Mvvm
{
  public sealed class CommonConfigurationViewModel : ReactiveObject, ICommonConfigurationViewModel
  {
    internal CommonConfigurationViewModel(IAppState appState)
    {
      var commonConfigurations = appState.UIComponents
        .Map(uic => uic.ViewModel is ICommonConfiguration commonConfiguration
          ? Some((UIC: uic, CommonConfiguration: commonConfiguration))
          : None
          )
        .Somes()
        .ToArr();

      ModuleViewModels = commonConfigurations.Map(
        cc => new ModuleViewModel(cc.UIC.DisplayName, cc.CommonConfiguration)
        );

      OK = ReactiveCommand.Create(HandleOK);
      Cancel = ReactiveCommand.Create(() => DialogResult = false);
    }

    public Arr<ModuleViewModel> ModuleViewModels { get; }

    public ICommand OK { get; }

    public ICommand Cancel { get; }

    public bool? DialogResult
    {
      get => _dialogResult;
      set => this.RaiseAndSetIfChanged(ref _dialogResult, value);
    }
    private bool? _dialogResult;


    private void HandleOK()
    {
      ModuleViewModels.Do(mvm => mvm.ApplyConfiguration());

      DialogResult = true;
    }
  }
}
