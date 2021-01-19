using LanguageExt;
using ReactiveUI;
using RVis.Base.Extensions;
using RVis.Model;
using RVis.Model.Extensions;
using RVisUI.Model;
using RVisUI.Model.Extensions;
using System;
using System.ComponentModel;
using System.Data;
using System.Reactive.Disposables;
using System.Windows.Input;
using static RVis.Base.Check;
using static RVis.Base.Extensions.LangExt;
using static RVis.Base.Extensions.NumExt;

namespace Sampling
{
  internal sealed class OutputsSelectedSampleViewModel : IOutputsSelectedSampleViewModel, INotifyPropertyChanged, IDisposable
  {
    internal OutputsSelectedSampleViewModel(IAppState appState, IAppService appService, ModuleState moduleState)
    {
      _appState = appState;
      _moduleState = moduleState;
      _simulation = appState.Target.AssertSome();

      ShareParameterValues = ReactiveCommand.Create(
        HandleShareParameterValues,
        this.ObservableForProperty(
          vm => vm.SelectedSample,
          _ => SelectedSample != NOT_FOUND
          )
        );

      _reactiveSafeInvoke = appService.GetReactiveSafeInvoke();

      _subscriptions = new CompositeDisposable(

        this
          .ObservableForProperty(vm => vm.SelectedSample)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveSelectedSample
              )
            )

        );
    }

    public int SelectedSample
    {
      get => _selectedSample;
      set => this.RaiseAndSetIfChanged(ref _selectedSample, value, PropertyChanged);
    }
    private int _selectedSample = NOT_FOUND;

    public string? SampleIdentifier
    {
      get => _sampleIdentifier;
      set => this.RaiseAndSetIfChanged(ref _sampleIdentifier, value, PropertyChanged);
    }
    private string? _sampleIdentifier;

    public Arr<string> ParameterValues
    {
      get => _parameterValues;
      set => this.RaiseAndSetIfChanged(ref _parameterValues, value, PropertyChanged);
    }
    private Arr<string> _parameterValues;

    public ICommand ShareParameterValues { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Dispose() =>
      Dispose(true);

    private void Dispose(bool disposing)
    {
      if (!_disposed)
      {
        if (disposing)
        {
          _subscriptions.Dispose();
        }

        _disposed = true;
      }
    }

    private void HandleShareParameterValues()
    {
      RequireNotNull(_moduleState.SamplingDesign);

      var defaultInput = _simulation.SimConfig.SimInput;

      var row = _moduleState.SamplingDesign.Samples.Rows[SelectedSample];

      var parameterStates = _moduleState.SamplingDesign.DesignParameters.Map(dp =>
      {
        var (_, minimum, maximum) = _appState.SimSharedState.ParameterSharedStates.GetParameterValueStateOrDefaults(
          dp.Name,
          defaultInput.SimParameters
          );

        var value = row.Field<double>(dp.Name);
        if (value < minimum) minimum = value.GetPreviousOrderOfMagnitude();
        if (value > maximum) maximum = value.GetNextOrderOfMagnitude();

        return (dp.Name, value, minimum, maximum, NoneOf<IDistribution>());
      });

      _appState.SimSharedState.ShareParameterState(parameterStates);
    }

    private void ObserveSelectedSample(object _) =>
      PopulateSelectedSample();

    private void PopulateSelectedSample()
    {
      if (SelectedSample == NOT_FOUND)
      {
        SampleIdentifier = default;
        ParameterValues = default;
        return;
      }

      RequireNotNull(_moduleState.SamplingDesign);

      SampleIdentifier = $"Sample #{SelectedSample + 1}";

      var row = _moduleState.SamplingDesign.Samples.Rows[SelectedSample];
      ParameterValues = _moduleState.SamplingDesign.DesignParameters
        .Map(dp => _simulation.SimConfig.SimInput.SimParameters.GetParameter(dp.Name))
        .Map(p => $"{p.Name} = {row.Field<double>(p.Name):G4} {p.Unit}")
        .ToArr();
    }

    private readonly IAppState _appState;
    private readonly ModuleState _moduleState;
    private readonly Simulation _simulation;
    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
    private readonly IDisposable _subscriptions;
    private bool _disposed = false;
  }
}
