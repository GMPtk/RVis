using LanguageExt;
using RVisUI.AppInf;
using static LanguageExt.Prelude;

#nullable disable

namespace Sampling.Design
{
  internal sealed class OutputsEvidenceViewModel : IOutputsEvidenceViewModel
  {
    public Arr<ISelectableItemViewModel> Observations => Range('a', 'z')
      .Map(c => new SelectableItemViewModel<string>(
        default,
        "observations " + new string(c, c % 10),
        default,
        default,
        false
        )
      { IsSelected = 0 == c % 2 })
      .ToArr<ISelectableItemViewModel>();
  }
}
