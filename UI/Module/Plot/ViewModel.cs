using LanguageExt;
using ReactiveUI;
using RVis.Base;
using RVis.Base.Extensions;
using RVis.Model;
using RVis.Model.Extensions;
using RVisUI.Model;
using RVisUI.Model.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using static LanguageExt.Prelude;
using static RVis.Base.Check;
using static System.Double;
using static System.Globalization.CultureInfo;

namespace Plot
{
  internal sealed class ViewModel : IViewModel, ITaskRunnerContainer, ISharedStateProvider, ICommonConfiguration, IDisposable
  {
    internal ViewModel(IAppState appState, IAppService appService, IAppSettings appSettings)
    {
      _appState = appState;
      _sharedState = appState.SimSharedState;
      _simulation = appState.Target.AssertSome("No simulation");
      _evidence = appState.SimEvidence;

      _moduleState = ModuleState.LoadOrCreate(_simulation);

      _outputGroupStore = new OutputGroupStore(_simulation);

      _parameters = new Parameters(_simulation, appService, _moduleState);

      _traceViewModel = new TraceViewModel(appState, appService, appSettings, _parameters.ViewModels, _moduleState, _outputGroupStore);
      _parametersViewModel = new ParametersViewModel(_parameters.ViewModels);
      _outputsViewModel = new OutputsViewModel(appState, _outputGroupStore);

      _reactiveSafeInvoke = appService.GetReactiveSafeInvoke();

      _subscriptions = new CompositeDisposable(SubscribeToObservables());

      TraceViewModel.IsSelected = true;
    }

    public ITraceViewModel TraceViewModel => _traceViewModel;

    public IParametersViewModel ParametersViewModel => _parametersViewModel;

    public IOutputsViewModel OutputsViewModel => _outputsViewModel;

    public Arr<ITaskRunner> GetTaskRunners() => Arr.empty<ITaskRunner>();

    public void ApplyState(
      SimSharedStateApply applyType,
      Arr<(SimParameter Parameter, double Minimum, double Maximum, Option<IDistribution> Distribution)> parameterSharedStates,
      Arr<SimElement> elementSharedStates,
      Arr<SimObservations> observationsSharedStates
      )
    {
      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        if (applyType.IncludesParameters())
        {
          _moduleState.ParameterEditStates.Iter(pes =>
          {
            var maybeParameterSharedState = parameterSharedStates.Find(pss => pss.Parameter.Name == pes.Name);
            maybeParameterSharedState.Match(
              pss =>
              {
                pes.IsSelected = true;
                pes.Value = pss.Parameter.Value;
                pes.Minimum = pss.Minimum;
                pes.Maximum = pss.Maximum;
              },
              () => pes.IsSelected = applyType.IsSingle() && pes.IsSelected);
          });
        }

        if (applyType.IncludesOutputs())
        {
          if (applyType.IsSet())
          {
            _moduleState.TraceDataPlotStates.Iter((i, tdps) =>
            {
              if (i < elementSharedStates.Count)
              {
                tdps.DepVarConfigState.SelectedElementName = elementSharedStates[i].Name;
                tdps.IsVisible = true;
              }
              else
              {
                tdps.IsVisible = false;
              }
            });
          }
          else if (applyType.IsSingle())
          {
            var name = elementSharedStates.Head().Name;
            _moduleState.TraceDataPlotStates
              .Find(tdps => tdps.DepVarConfigState.SelectedElementName == name)
              .Match(
                tdps => tdps,
                () => _moduleState.TraceDataPlotStates.Find(tdps => tdps.IsVisible == false)
              )
              .IfSome(tdps =>
              {
                tdps.DepVarConfigState.SelectedElementName = name;
                tdps.IsVisible = true;
              });
          }
        }

        if (applyType.IncludesObservations())
        {
          var references = observationsSharedStates.Map(oss => _evidence.GetReference(oss));

          foreach (var traceDataPlotState in _moduleState.TraceDataPlotStates)
          {
            if (applyType.IsSet())
            {
              traceDataPlotState.DepVarConfigState.ObservationsReferences = references;
            }
            else if (applyType.IsSingle())
            {
              var reference = references.Head();
              if (!traceDataPlotState.DepVarConfigState.ObservationsReferences.Contains(reference))
              {
                traceDataPlotState.DepVarConfigState.ObservationsReferences =
                  traceDataPlotState.DepVarConfigState.ObservationsReferences.Add(reference);
              }
            }
          }
        }
      }
    }

    public void ShareState(ISimSharedStateBuilder sharedStateBuilder)
    {
      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        _moduleState.ParameterEditStates.Iter(pes =>
        {
          if (!pes.IsSelected) return;
          if (!TryParse(pes.Value, NumberStyles.Float, InvariantCulture, out double value)) return;

          RequireNotNullEmptyWhiteSpace(pes.Name);

          sharedStateBuilder.AddParameter(pes.Name, value, pes.Minimum, pes.Maximum, None);
        });

        var values = _simulation.SimConfig.SimOutput.SimValues;
        var elements = values.Bind(v => v.SimElements);

        _moduleState.TraceDataPlotStates.Iter(tdps =>
        {
          if (!tdps.IsVisible) return;
          var name = tdps.DepVarConfigState.SelectedElementName;
          var isElement = elements.Exists(e => e.Name == name);
          if (isElement) sharedStateBuilder.AddOutput(name!);

          tdps.DepVarConfigState.ObservationsReferences.Iter(
            sharedStateBuilder.AddObservations
            );
        });
      }
    }

    bool? ICommonConfiguration.AutoApplyParameterSharedState
    {
      get => _moduleState.AutoApplyParameterSharedState;
      set => _moduleState.AutoApplyParameterSharedState = value;
    }

    bool? ICommonConfiguration.AutoShareParameterSharedState
    {
      get => _moduleState.AutoShareParameterSharedState;
      set => _moduleState.AutoShareParameterSharedState = value;
    }

    bool? ICommonConfiguration.AutoApplyElementSharedState
    {
      get => _moduleState.AutoApplyElementSharedState;
      set => _moduleState.AutoApplyElementSharedState = value;
    }

    bool? ICommonConfiguration.AutoShareElementSharedState
    {
      get => _moduleState.AutoShareElementSharedState;
      set => _moduleState.AutoShareElementSharedState = value;
    }

    bool? ICommonConfiguration.AutoApplyObservationsSharedState
    {
      get => _moduleState.AutoApplyObservationsSharedState;
      set => _moduleState.AutoApplyObservationsSharedState = value;
    }

    bool? ICommonConfiguration.AutoShareObservationsSharedState
    {
      get => _moduleState.AutoShareObservationsSharedState;
      set => _moduleState.AutoShareObservationsSharedState = value;
    }

    public void Dispose() => Dispose(true);

    private void Dispose(bool disposing)
    {
      if (!_disposed)
      {
        if (disposing)
        {
          _subscriptions.Dispose();

          _traceViewModel.Dispose();
          _parametersViewModel.Dispose();
          _outputsViewModel.Dispose();
          _parameters.Dispose();

          _outputGroupStore.Save();
          _outputGroupStore.Dispose();

          ModuleState.Save(_moduleState, _simulation);
        }
        _disposed = true;
      }
    }

    private void ObserveParameterEditStatePropertyChanged(ParameterEditState parameterEditState)
    {
      if (!_moduleState.AutoShareParameterSharedState.IsTrue()) return;

      if (parameterEditState.IsSelected)
      {
        if (TryParse(parameterEditState.Value, NumberStyles.Float, InvariantCulture, out double value))
        {
          RequireNotNullEmptyWhiteSpace(parameterEditState.Name);

          var distribution = _sharedState.ParameterSharedStates
            .Find(pss => pss.Name == parameterEditState.Name)
            .Match(pss => pss.Distribution, () => None);

          _sharedState.ShareParameterState(
            parameterEditState.Name,
            value,
            parameterEditState.Minimum,
            parameterEditState.Maximum,
            distribution
            );
        }
      }
      else
      {
        RequireNotNullEmptyWhiteSpace(parameterEditState.Name);

        _sharedState.UnshareParameterState(parameterEditState.Name);
      }
    }

    private void ObserveDepVarConfigStateSelectedElementName(DepVarConfigState depVarConfigState)
    {
      if (!_moduleState.AutoShareElementSharedState.IsTrue()) return;

      RequireNotNullEmptyWhiteSpace(depVarConfigState.SelectedElementName);

      _sharedState.ShareElementState(depVarConfigState.SelectedElementName);
    }

    private void ObserveDepVarConfigStateObservationsReferences(DepVarConfigState depVarConfigState)
    {
      if (!_moduleState.AutoShareObservationsSharedState.IsTrue()) return;

      _sharedState.ShareObservationsState(depVarConfigState.ObservationsReferences);
    }

    private void ObserveEvidenceObservationsChange((Arr<SimObservations> Observations, ObservableQualifier ObservableQualifier) change)
    {
      if (change.ObservableQualifier != ObservableQualifier.Remove) return;

      var references = change.Observations.Map(o => _evidence.GetReference(o));

      foreach (var traceDataPlotState in _moduleState.TraceDataPlotStates)
      {
        var withoutRemoved = traceDataPlotState.DepVarConfigState.ObservationsReferences.Filter(r => !references.Contains(r));
        if (withoutRemoved.Count < traceDataPlotState.DepVarConfigState.ObservationsReferences.Count)
        {
          traceDataPlotState.DepVarConfigState.ObservationsReferences = withoutRemoved;
        }
      }
    }

    private void ObserveSharedStateParameterChange((Arr<SimParameterSharedState> ParameterSharedStates, ObservableQualifier ObservableQualifier) change)
    {
      if (!_moduleState.AutoApplyParameterSharedState.IsTrue()) return;

      change.ParameterSharedStates.Iter(pss =>
      {
        var parameterEditState = _moduleState.ParameterEditStates
          .Find(pes => pes.Name == pss.Name)
          .AssertSome($"Unknown parameter shared state: {pss.Name}");

        if (change.ObservableQualifier.IsAdd())
        {
          parameterEditState.IsSelected = true;
          parameterEditState.Value = pss.Value.ToString(InvariantCulture);
          parameterEditState.Minimum = pss.Minimum;
          parameterEditState.Maximum = pss.Maximum;
        }
        else if (change.ObservableQualifier.IsRemove())
        {
          parameterEditState.IsSelected = false;
        }
      });
    }

    private void ObserveSharedStateElementChange((Arr<SimElementSharedState> ElementSharedStates, ObservableQualifier ObservableQualifier) change)
    {
      if (!_moduleState.AutoApplyElementSharedState.IsTrue()) return;
      if (change.ObservableQualifier != ObservableQualifier.Add) return;

      var toApply = change.ElementSharedStates
        .Take(_moduleState.TraceDataPlotStates.Count)
        .ToArr();

      toApply.Iter((i, ess) =>
      {
        _moduleState.TraceDataPlotStates[i].DepVarConfigState.SelectedElementName = ess.Name;
      });
    }

    private void ObserveSharedStateObservationsChange((Arr<SimObservationsSharedState> ObservationsSharedStates, ObservableQualifier ObservableQualifier) change)
    {
      if (!_moduleState.AutoApplyObservationsSharedState.IsTrue()) return;

      var observations = change.ObservationsSharedStates
        .Map(oss => _evidence.GetObservations(oss.Reference))
        .Somes()
        .ToArr();

      var references = change.ObservationsSharedStates.Map(oss => oss.Reference);

      foreach (var traceDataPlotState in _moduleState.TraceDataPlotStates)
      {
        if (change.ObservableQualifier.IsAdd())
        {
          var toAdd = references.Filter(
            r => !traceDataPlotState.DepVarConfigState.ObservationsReferences.Contains(r)
            );

          if (!toAdd.IsEmpty)
          {
            traceDataPlotState.DepVarConfigState.ObservationsReferences += toAdd;
          }
        }
        else if (change.ObservableQualifier.IsRemove())
        {
          var withoutRemoved = traceDataPlotState.DepVarConfigState.ObservationsReferences.Filter(r => !references.Contains(r));
          if (withoutRemoved.Count < traceDataPlotState.DepVarConfigState.ObservationsReferences.Count)
          {
            traceDataPlotState.DepVarConfigState.ObservationsReferences = withoutRemoved;
          }
        }
      }
    }

    private void ObserveActivatedOutputGroup((OutputGroup OutputGroup, bool) activatedOutputGroup)
    {
      if (TraceViewModel.IsSelected) return;

      var (outputGroup, _) = activatedOutputGroup;

      _moduleState.ParameterEditStates.Iter(pes =>
      {
        var inputAssignment = outputGroup.InputAssignments.Find(p => p.Name == pes.Name);
        inputAssignment.Match(p =>
        {
          pes.Value = p.Value;
          pes.IsSelected = true;
        },
        () => pes.IsSelected = false);
      });

      TraceViewModel.PlotWorkingChanges.Execute(default);
      TraceViewModel.IsSelected = true;
    }

    private void ObserveOutputRequest(SimDataItem<OutputRequest> item)
    {
      if (item.IsSimDataEvent(out SimDataEvent _)) return;
      if (item.RequestToken is not DataRequestType dataRequestType) return;
      if (dataRequestType != DataRequestType.LogEntry) return;
      if (item.Item.SerieInputs.IsLeft) return;

      var edits = item.Simulation.SimConfig.SimInput.GetEdits(item.Item.SeriesInput);

      _moduleState.ParameterEditStates.Iter(pes =>
      {
        var edit = edits.Find(p => p.Name == pes.Name);
        edit.Match(p =>
        {
          pes.Value = p.Value;
          pes.IsSelected = true;
        },
        () => pes.IsSelected = false);
      });

      TraceViewModel.IsSelected = true;
    }

    private IEnumerable<IDisposable> SubscribeToObservables()
    {
      RequireNotNull(SynchronizationContext.Current);

      var subscriptions = _moduleState.ParameterEditStates
        .Map(pes => pes
          .GetWhenPropertyChanged()
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<string?>(
              _ => ObserveParameterEditStatePropertyChanged(pes)
              )
            )
        )
        .ToList();

      subscriptions.AddRange(
        _moduleState.TraceDataPlotStates.Map(
          tdps => tdps.DepVarConfigState
            .ObservableForProperty(dvcs => dvcs.SelectedElementName)
            .Subscribe(
              _reactiveSafeInvoke.SuspendAndInvoke<object>(
                _ => ObserveDepVarConfigStateSelectedElementName(tdps.DepVarConfigState)
                )
              )
          )
        );

      subscriptions.AddRange(
        _moduleState.TraceDataPlotStates.Map(
          tdps => tdps.DepVarConfigState
            .ObservableForProperty(dvcs => dvcs.ObservationsReferences)
            .Subscribe(
              _reactiveSafeInvoke.SuspendAndInvoke<object>(
                _ => ObserveDepVarConfigStateObservationsReferences(tdps.DepVarConfigState)
                )
             )
          )
        );

      subscriptions.Add(
        _evidence.ObservationsChanges.Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<(Arr<SimObservations>, ObservableQualifier)>(
            ObserveEvidenceObservationsChange
            )
          )
        );

      subscriptions.Add(
        _sharedState.ParameterSharedStateChanges.Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<(Arr<SimParameterSharedState>, ObservableQualifier)>(
            ObserveSharedStateParameterChange
            )
          )
        );

      subscriptions.Add(
        _sharedState.ElementSharedStateChanges.Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<(Arr<SimElementSharedState>, ObservableQualifier)>(
            ObserveSharedStateElementChange
            )
          )
        );

      subscriptions.Add(
        _sharedState.ObservationsSharedStateChanges.Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<(Arr<SimObservationsSharedState>, ObservableQualifier)>(
            ObserveSharedStateObservationsChange
            )
          )
        );

      subscriptions.Add(
        _outputGroupStore.ActivatedOutputGroups
          .ObserveOn(SynchronizationContext.Current)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<(OutputGroup, bool)>(
              ObserveActivatedOutputGroup
            )
          )
        );

      subscriptions.Add(
        _appState.SimData.OutputRequests
          .ObserveOn(SynchronizationContext.Current)
          .Subscribe(ObserveOutputRequest)
        );

      return subscriptions;
    }

    private readonly IAppState _appState;
    private readonly ISimSharedState _sharedState;
    private readonly Simulation _simulation;
    private readonly ISimEvidence _evidence;
    private readonly ModuleState _moduleState;
    private readonly OutputGroupStore _outputGroupStore;
    private readonly Parameters _parameters;
    private readonly TraceViewModel _traceViewModel;
    private readonly ParametersViewModel _parametersViewModel;
    private readonly OutputsViewModel _outputsViewModel;
    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
    private readonly IDisposable _subscriptions;
    private bool _disposed = false;
  }
}
