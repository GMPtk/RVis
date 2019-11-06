using LanguageExt;
using ReactiveUI;
using System.Windows.Input;

namespace RVisUI.Mvvm
{
  internal class SelectExecViewModel : ReactiveObject, ISelectExecViewModel
  {
    internal SelectExecViewModel(
      Arr<string> unaryFunctions,
      int unaryFunctionSelectedIndex,
      Arr<string> scalarSets,
      int scalarSetSelectedIndex
      )
    {
      UnaryFunctions = unaryFunctions.Map(s => s.Replace("_", "__"));
      UnaryFunctionSelectedIndex = unaryFunctionSelectedIndex;

      ScalarSets = scalarSets.Map(s => s.Replace("_", "__"));
      ScalarSetSelectedIndex = scalarSetSelectedIndex;

      OK = ReactiveCommand.Create(
        () => DialogResult = true,
        this.WhenAny(
          vm => vm.UnaryFunctionSelectedIndex, 
          vm => vm.ScalarSetSelectedIndex, 
          (i,j) => i.Value >= 0 && j.Value >= 0
          )
        );
      Cancel = ReactiveCommand.Create(() => DialogResult = false);
    }

    public Arr<string> UnaryFunctions { get; }

    public int UnaryFunctionSelectedIndex
    {
      get => _unaryFunctionSelectedIndex;
      set => this.RaiseAndSetIfChanged(ref _unaryFunctionSelectedIndex, value);
    }
    private int _unaryFunctionSelectedIndex;

    public Arr<string> ScalarSets { get; }

    public int ScalarSetSelectedIndex
    {
      get => _scalarSetSelectedIndex;
      set => this.RaiseAndSetIfChanged(ref _scalarSetSelectedIndex, value);
    }
    private int _scalarSetSelectedIndex;

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
