using LanguageExt;
using ReactiveUI;
using RVis.Base.Extensions;
using RVis.Model;
using RVis.Model.Extensions;
using RVisUI.Model;
using RVisUI.Model.Extensions;
using System;
using System.Globalization;
using System.Reactive.Disposables;
using System.Windows.Input;
using static RVis.Base.Check;
using static System.Double;
using static System.Globalization.CultureInfo;

namespace Plot
{
  internal class Parameters : IDisposable
  {
    internal Parameters(Simulation simulation, IAppService appService, ModuleState moduleState)
    {
      _simulation = simulation;
      _appService = appService;
      _moduleState = moduleState;

      _toggleSelect = ReactiveCommand.Create<IParameterViewModel>(HandleToggleSelect);
      _resetValue = ReactiveCommand.Create<IParameterViewModel>(HandleResetValue);
      _increaseMinimum = ReactiveCommand.Create<IParameterViewModel>(HandleIncreaseMinimum);
      _decreaseMinimum = ReactiveCommand.Create<IParameterViewModel>(HandleDecreaseMinimum);
      _increaseMaximum = ReactiveCommand.Create<IParameterViewModel>(HandleIncreaseMaximum);
      _decreaseMaximum = ReactiveCommand.Create<IParameterViewModel>(HandleDecreaseMaximum);

      var parameters = simulation.SimConfig.SimInput.SimParameters;

      ViewModels = _moduleState.ParameterEditStates.Map(
        pes =>
        {
          var parameter = parameters.GetParameter(pes.Name.AssertNotNull());

          return new ParameterViewModel(
            _toggleSelect,
            _resetValue,
            _increaseMinimum,
            _decreaseMinimum,
            _increaseMaximum,
            _decreaseMaximum,
            parameter.Name,
            parameter.Value,
            parameter.Unit,
            parameter.Description,
            appService
            )
          {
            IsSelected = pes.IsSelected,
            TValue = pes.Value,
            Minimum = pes.Minimum,
            Maximum = pes.Maximum
          };
        });

      _reactiveSafeInvoke = appService.GetReactiveSafeInvoke();

      var disposables = moduleState.ParameterEditStates
        .Map(pes => pes
          .GetWhenPropertyChanged()
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<string?>(
              s => ObserveParameterEditStateProperty(pes, s)
              )
            )
          );

      disposables += ViewModels.Map(
        p => p
          .GetWhenPropertyChanged()
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<string?>(s => ObserveParameterViewModelProperty(p, s))
            )
        );

      _subscriptions = new CompositeDisposable(disposables);
    }

    public Arr<ParameterViewModel> ViewModels { get; }

    public void Dispose() => Dispose(true);

    private void ObserveParameterEditStateProperty(ParameterEditState parameterEditState, string? propertyName)
    {
      var parameterViewModel = ViewModels
        .Find(p => p.Name == parameterEditState.Name)
        .AssertSome();

      switch (propertyName)
      {
        case nameof(ParameterEditState.IsSelected):
          parameterViewModel.IsSelected = parameterEditState.IsSelected;
          break;

        case nameof(ParameterEditState.Value):
          if (TryParse(parameterEditState.Value, NumberStyles.Float, InvariantCulture, out double d) &&
              d.IsInClosedInterval(parameterEditState.Minimum, parameterEditState.Maximum))
          {
            parameterViewModel.Set(d, parameterEditState.Minimum, parameterEditState.Maximum);
          }
          else
          {
            parameterViewModel.TValue = parameterEditState.Value;
          }
          break;

        case nameof(ParameterEditState.Minimum):
          parameterViewModel.Minimum = parameterEditState.Minimum;
          break;

        case nameof(ParameterEditState.Maximum):
          parameterViewModel.Maximum = parameterEditState.Maximum;
          break;
      }
    }

    private void ObserveParameterViewModelProperty(ParameterViewModel parameterViewModel, string? propertyName)
    {
      var parameterEditState = _moduleState.ParameterEditStates
        .Find(pes => pes.Name == parameterViewModel.Name)
        .AssertSome();

      switch (propertyName)
      {
        case nameof(ParameterViewModel.IsSelected):
          parameterEditState.IsSelected = parameterViewModel.IsSelected;
          break;

        case nameof(ParameterViewModel.TValue):
        case nameof(ParameterViewModel.Minimum):
        case nameof(ParameterViewModel.Maximum):
          if (parameterViewModel.TValue.IsAString())
          {
            if (!TryParse(parameterViewModel.TValue, out double d) || d.IsInClosedInterval(parameterViewModel.Minimum, parameterViewModel.Maximum))
            {
              parameterEditState.Value = parameterViewModel.TValue;
              parameterEditState.Minimum = parameterViewModel.Minimum;
              parameterEditState.Maximum = parameterViewModel.Maximum;
            }
          }
          break;
      }
    }

    private void HandleToggleSelect(IParameterViewModel parameterViewModel)
    {
      parameterViewModel.IsSelected = !parameterViewModel.IsSelected;
    }

    private void HandleResetValue(IParameterViewModel parameterViewModel)
    {
      RequireTrue(parameterViewModel.IsSelected);

      if (parameterViewModel.NValue.HasValue)
      {
        // have value so request is for slider reset
        var value = parameterViewModel.NValue.Value;
        var minimum = value.GetPreviousOrderOfMagnitude();
        var maximum = value.GetNextOrderOfMagnitude();
        parameterViewModel.Set(value, minimum, maximum);
      }
      else
      {
        // non-numerical text, or no text at all...

        var currentTValue = parameterViewModel.TValue;
        if (currentTValue.IsAString())
        {
          // handle one specific non-numeric case
          var valid = false; // want "min < val < max"

          var parts = currentTValue.Split('<');

          if (parts.Length == 3)
          {
            var validMinimum = TryParse(parts[0], out double minimum);
            var validValue = TryParse(parts[1], out double value);
            var validMaximum = TryParse(parts[2], out double maximum);
            valid = validMinimum && validValue && validMaximum && minimum < value && value < maximum;
            if (valid) parameterViewModel.Set(value, minimum, maximum);
          }

          if (!valid)
          {
            _appService.Notify(
              NotificationType.Error,
              nameof(ParameterViewModel),
              nameof(HandleResetValue),
              "Invalid interval. Format is min < val < max."
              );
          }
        }
        else
        {
          // no entry so user wants original back
          parameterViewModel.TValue = parameterViewModel.DefaultValue;
        }
      }
    }

    private void HandleIncreaseMinimum(IParameterViewModel parameterViewModel)
    {
      var minimum = parameterViewModel.Minimum;
      var nextOOM = minimum.GetNextOrderOfMagnitude();
      if (!parameterViewModel.NValue.HasValue || nextOOM < parameterViewModel.NValue)
      {
        parameterViewModel.Minimum = nextOOM;
      }
    }

    private void HandleDecreaseMinimum(IParameterViewModel parameterViewModel)
    {
      var minimum = parameterViewModel.Minimum;
      var previousOOM = minimum.GetPreviousOrderOfMagnitude();
      parameterViewModel.Minimum = previousOOM;
    }

    private void HandleIncreaseMaximum(IParameterViewModel parameterViewModel)
    {
      var maximum = parameterViewModel.Maximum;
      var nextOOM = maximum.GetNextOrderOfMagnitude();
      parameterViewModel.Maximum = nextOOM;
    }

    private void HandleDecreaseMaximum(IParameterViewModel parameterViewModel)
    {
      var maximum = parameterViewModel.Maximum;
      var previousOOM = maximum.GetPreviousOrderOfMagnitude();
      if (!parameterViewModel.NValue.HasValue || previousOOM > parameterViewModel.NValue)
      {
        parameterViewModel.Maximum = previousOOM;
      }
    }

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

    private readonly Simulation _simulation;
    private readonly IAppService _appService;
    private readonly ModuleState _moduleState;
    private readonly IDisposable _subscriptions;
    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;

    private readonly ICommand _toggleSelect;
    private readonly ICommand _resetValue;
    private readonly ICommand _increaseMinimum;
    private readonly ICommand _decreaseMinimum;
    private readonly ICommand _increaseMaximum;
    private readonly ICommand _decreaseMaximum;

    private bool _disposed = false;
  }
}
