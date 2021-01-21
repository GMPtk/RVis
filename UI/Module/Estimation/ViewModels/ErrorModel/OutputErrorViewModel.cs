using LanguageExt;
using ReactiveUI;
using RVis.Base.Extensions;
using RVis.Model;
using RVisUI.Model;
using RVisUI.Model.Extensions;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using static RVis.Base.Check;
using static RVis.Base.Extensions.NumExt;

namespace Estimation
{
  internal sealed class OutputErrorViewModel : IOutputErrorViewModel, INotifyPropertyChanged, IDisposable
  {
    internal OutputErrorViewModel(
      IAppState appState,
      IAppService appService
      )
      : this(appState, appService, ErrorModelType.All)
    {
    }

    internal OutputErrorViewModel(
      IAppState appState,
      IAppService appService,
      ErrorModelType errorModelsInView
      )
    {
      var simulation = appState.Target.AssertSome();
      _elements = simulation.SimConfig.SimOutput.SimValues.Bind(v => v.SimElements);

      var errorModelTypes = ErrorModel.GetErrorModelTypes(errorModelsInView);

      var errorViewModelTypes = errorModelTypes.Map(
        dt => typeof(IErrorViewModel).Assembly
          .GetType($"{nameof(Estimation)}.{dt}ErrorViewModel")
          .AssertNotNull($"{nameof(Estimation)}.{dt}ErrorViewModel not found")
        );

      _errorViewModels = errorViewModelTypes
        .Select(t => Activator.CreateInstance(t, BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { appService }, null))
        .Cast<IErrorViewModel>()
        .ToArr();

      var displayNames = errorViewModelTypes.Select(
        t => t.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? t.Name
        );
      ErrorModelNames = displayNames.ToArr();

      _reactiveSafeInvoke = appService.GetReactiveSafeInvoke();

      this.ObservableForProperty(vm => vm.SelectedErrorModelName).Subscribe(
        _reactiveSafeInvoke.SuspendAndInvoke<object>(
          ObserveSelectedErrorModelName
          )
        );

      _errorViewModels.Iter(dvm => ((INotifyPropertyChanged)dvm)
        .GetWhenPropertyChanged()
        .Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<string?>(
            pn => ObserveErrorViewModelProperty(dvm, pn)
          )
        )
      );

      this.ObservableForProperty(vm => vm.OutputState).Subscribe(
        _reactiveSafeInvoke.SuspendAndInvoke<object>(
          ObserveOutputState
          )
        );
    }

    public Arr<string> ErrorModelNames { get; }

    public int SelectedErrorModelName
    {
      get => _selectedErrorModelName;
      set => this.RaiseAndSetIfChanged(ref _selectedErrorModelName, value, PropertyChanged);
    }
    private int _selectedErrorModelName = NOT_FOUND;

    public IErrorViewModel? ErrorViewModel
    {
      get => _errorViewModel;
      set => this.RaiseAndSetIfChanged(ref _errorViewModel, value, PropertyChanged);
    }
    private IErrorViewModel? _errorViewModel;

    public Option<OutputState> OutputState
    {
      get => _outputState;
      set => this.RaiseAndSetIfChanged(ref _outputState, value, PropertyChanged);
    }
    private Option<OutputState> _outputState;

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Dispose() => Dispose(true);

    private void ObserveSelectedErrorModelName(object _)
    {
      RequireTrue(SelectedErrorModelName.IsFound());

      var outputState = _outputState.AssertSome();

      ShowSelectedErrorModel(outputState.Name, outputState.ErrorModels);

      OutputState = new OutputState(
        outputState.Name,
        ErrorViewModel!.ErrorModelType,
        outputState.ErrorModels,
        outputState.IsSelected
        );
    }

    private void ObserveErrorViewModelProperty(IErrorViewModel errorViewModel, string? propertyName)
    {
      if (propertyName != nameof(IErrorViewModel<NormalErrorModel>.ErrorModel)) return;

      RequireNotNull(errorViewModel.ErrorModelUnsafe);

      var outputState = _outputState.AssertSome();
      RequireTrue(outputState.IsSelected);
      RequireTrue(outputState.ErrorModelType == errorViewModel.ErrorModelType);

      var index = outputState.ErrorModels.FindIndex(
        d => d.ErrorModelType == errorViewModel.ErrorModelType
        );

      RequireTrue(index.IsFound());

      var errorModels = outputState.ErrorModels.SetItem(
        index,
        errorViewModel.ErrorModelUnsafe
        );

      OutputState = new OutputState(
        outputState.Name,
        outputState.ErrorModelType,
        errorModels,
        outputState.IsSelected
        );
    }

    private void ObserveOutputState(object _)
    {
      void Some(OutputState outputState)
      {
        RequireFalse(outputState.ErrorModelType == ErrorModelType.None);

        var index = _errorViewModels.FindIndex(
          evm => evm.ErrorModelType == outputState.ErrorModelType
          );
        RequireTrue(index.IsFound());
        SelectedErrorModelName = index;

        ShowSelectedErrorModel(outputState.Name, outputState.ErrorModels);
      }

      void None()
      {
        SelectedErrorModelName = NOT_FOUND;
        ErrorViewModel = default;
      }

      _outputState.Match(Some, None);
    }

    private void Dispose(bool disposing)
    {
      if (!_disposed)
      {
        if (disposing)
        {
          _errorViewModels.Iter(dvm => dvm.Dispose());
        }

        _disposed = true;
      }
    }

    private void ShowSelectedErrorModel(string outputName, Arr<IErrorModel> errorModels)
    {
      var element = _elements
        .Find(e => e.Name == outputName)
        .AssertSome($"Unknown output {outputName}");

      var errorViewModel = _errorViewModels[SelectedErrorModelName];

      var errorModel = errorModels
        .Find(d => d.ErrorModelType == errorViewModel.ErrorModelType)
        .AssertSome();

      errorViewModel.ErrorModelUnsafe = default;
      errorViewModel.Variable = outputName;
      errorViewModel.Unit = element.Unit;
      errorViewModel.ErrorModelUnsafe = errorModel;

      ErrorViewModel = errorViewModel;
    }

    private readonly Arr<SimElement> _elements;
    private readonly Arr<IErrorViewModel> _errorViewModels;
    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
    private bool _disposed = false;
  }
}
