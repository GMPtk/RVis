using LanguageExt;
using System;
using System.Windows.Input;
using static LanguageExt.Prelude;

namespace RVisUI.Mvvm.Design
{
  public sealed class AcatHostViewModel : IAcatHostViewModel
  {
    public bool IsVisible => true;

    public ICommand Import => throw new NotImplementedException();

    public ICommand Load => throw new NotImplementedException();

    public bool CanConfigure => false;

    public Arr<IAcatViewModel> AcatViewModels => _acatViewModels;

    public IAcatViewModel? SelectedAcatViewModel { get => _acatViewModels[1]; set => throw new NotImplementedException(); }

    private readonly Arr<IAcatViewModel> _acatViewModels = Array<IAcatViewModel>(
      new DrugXSimpleAcatViewModel(),
      new DrugXNotSoSimpleAcatViewModel()
      );
  }
}
