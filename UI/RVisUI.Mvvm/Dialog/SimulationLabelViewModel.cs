using ReactiveUI;
using System.Windows.Input;

namespace RVisUI.Mvvm
{
  public class SimulationLabelViewModel : ReactiveObject, ISimulationLabelViewModel
  {
    public SimulationLabelViewModel()
    {
      OK = ReactiveCommand.Create(
        () => DialogResult = true,
        this.ObservableForProperty(vm => vm.Name, _ => Name?.Length > 0)
      );
      Cancel = ReactiveCommand.Create(() => DialogResult = false);
    }

    public string? Name
    {
      get => _targetSymbol;
      set => this.RaiseAndSetIfChanged(ref _targetSymbol, value);
    }
    private string? _targetSymbol;

    public string? Description
    {
      get => _description;
      set => this.RaiseAndSetIfChanged(ref _description, value);
    }
    private string? _description;

    public ICommand OK { get; }

    public ICommand Cancel { get; }

    public bool? DialogResult
    {
      get => _dialogResult;
      set => this.RaiseAndSetIfChanged(ref _dialogResult, value);
    }
    private bool? _dialogResult;
  }
}
