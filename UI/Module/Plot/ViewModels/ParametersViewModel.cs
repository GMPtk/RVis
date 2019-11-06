using LanguageExt;
using ReactiveUI;
using RVisUI.Model;
using System;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using static RVis.Base.Check;

namespace Plot
{
  internal class ParametersViewModel : ViewModelBase, IParametersViewModel
  {
    internal ParametersViewModel(Arr<ParameterViewModel> viewModels)
    {
      UnselectedParameters = new ObservableCollection<IParameterViewModel>(
        viewModels.Filter(vm => !vm.IsSelected)
        );

      SelectedParameters = new ObservableCollection<IParameterViewModel>(
        viewModels.Filter(vm => vm.IsSelected)
        );

      var disposables = viewModels.Map(
        p => p
          .ObservableForProperty(vm => vm.IsSelected)
          .Subscribe(
            _ => ObserveParameterViewModelIsSelected(p)
            )
        );

      _subscriptions = new CompositeDisposable(disposables);
    }

    public ObservableCollection<IParameterViewModel> UnselectedParameters { get; }

    public ObservableCollection<IParameterViewModel> SelectedParameters { get; }

    protected override void Dispose(bool disposing)
    {
      if (disposing)
      {
        _subscriptions.Dispose();
      }

      base.Dispose(disposing);
    }

    private void ObserveParameterViewModelIsSelected(ParameterViewModel parameterViewModel)
    {
      if (parameterViewModel.IsSelected)
      {
        RequireTrue(UnselectedParameters.Remove(parameterViewModel));
        SelectedParameters.Add(parameterViewModel);
      }
      else
      {
        RequireTrue(SelectedParameters.Remove(parameterViewModel));
        UnselectedParameters.Add(parameterViewModel);
      }
    }

    private readonly IDisposable _subscriptions;
  }
}
