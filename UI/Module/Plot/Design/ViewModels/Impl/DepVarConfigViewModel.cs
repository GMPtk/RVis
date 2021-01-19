using LanguageExt;
using RVisUI.AppInf;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using static LanguageExt.Prelude;

#nullable disable

namespace Plot.Design
{
  public sealed class DepVarConfigViewModel : IDepVarConfigViewModel
  {
    public ICommand ToggleView => throw new NotImplementedException();

    public bool IsViewOpen { get => false; set => throw new NotImplementedException(); }

    public ISelectableItemViewModel SelectedElement
    {
      get => new SelectableItemViewModel<string>(default, "A short name [unit]", default, default, true);
      set => throw new NotImplementedException();
    }

    public ObservableCollection<ISelectableItemViewModel> MRUElements => new ObservableCollection<ISelectableItemViewModel>(
      new[]
      {
        new SelectableItemViewModel<string>(default, "A short name", default, default, false),
        new SelectableItemViewModel<string>(default, "A very, very, very long name", default, default, false),
        new SelectableItemViewModel<string>(default, "Short", default, default, false),
      });

    public ObservableCollection<ISelectableItemViewModel> LRUElements => new ObservableCollection<ISelectableItemViewModel>(
      Range('a', 'z').Map(c => new SelectableItemViewModel<string>(default, new string(c, c % 10), default, default, false)).ToArr<ISelectableItemViewModel>()
      );

    public Arr<string> InsetOptions => Range(1, 30).Map(i => $"InsetOption{new string((char)('A' + (i % 26)), i % 8 + 1)}").ToArr();

    public int SelectedInsetOption { get => 2; set => throw new NotImplementedException(); }

    public Arr<ISelectableItemViewModel> SupplementaryElements
    {
      get => Range(1, 30).Map(i => new SelectableItemViewModel<string>(default, $"supplementary{i:0000}", default, default, false) { IsSelected = 0 == i % 2 }).ToArr<ISelectableItemViewModel>();
      set => throw new NotImplementedException();
    }

    public Arr<ISelectableItemViewModel> Observations
    {
      get => Range('a', 'z').Map(c => new SelectableItemViewModel<string>(default, "observations " + new string(c, c % 10), default, default, false) { IsSelected = 0 == c % 2 }).ToArr<ISelectableItemViewModel>();
      set => throw new NotImplementedException();
    }

    public bool IsScaleLogarithmic { get => true; set => throw new NotImplementedException(); }
  }
}
