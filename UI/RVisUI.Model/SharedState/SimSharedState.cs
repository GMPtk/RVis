using LanguageExt;
using RVis.Base;
using RVis.Base.Extensions;
using RVis.Model;
using RVis.Model.Extensions;
using RVisUI.Model.Extensions;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using static LanguageExt.Prelude;
using static RVis.Base.Check;

namespace RVisUI.Model
{
  public sealed class SimSharedState : ISimSharedState, IDisposable
  {
    private class _SimParameterSharedStateDTO
    {
      public string? Name { get; set; }
      public double Value { get; set; }
      public double Minimum { get; set; }
      public double Maximum { get; set; }
      public string? Distribution { get; set; }
    }

    private class _SimSharedStateDTO
    {
      public _SimParameterSharedStateDTO[] ParameterSharedStates { get; set; } = System.Array.Empty<_SimParameterSharedStateDTO>();
      public string[] ElementSharedStates { get; set; } = System.Array.Empty<string>();
      public string[] ObservationsSharedStates { get; set; } = System.Array.Empty<string>();
    }

    public void StartSharing(Simulation simulation)
    {
      RequireTrue(ParameterSharedStates.IsEmpty);
      RequireTrue(ElementSharedStates.IsEmpty);
      RequireTrue(ObservationsSharedStates.IsEmpty);

      try
      {
        var maybeDTO = simulation.LoadPrivateData<_SimSharedStateDTO>(
          nameof(RVisUI),
          nameof(Model),
          nameof(SimSharedState)
          );

        maybeDTO.IfSome(dto =>
        {
          ParameterSharedStates = dto.ParameterSharedStates
            .Select(pdto => new SimParameterSharedState(
              pdto.Name.AssertNotNull(),
              pdto.Value,
              pdto.Minimum,
              pdto.Maximum,
              Distribution.DeserializeDistribution(pdto.Distribution)
              )
            )
            .ToArr();

          ElementSharedStates = dto.ElementSharedStates
            .Select(s => new SimElementSharedState(s))
            .ToArr();

          ObservationsSharedStates = dto.ObservationsSharedStates
            .Select(s => new SimObservationsSharedState(s))
            .ToArr();
        });
      }
      finally
      {
        _simulation = simulation;
      }
    }

    public void StopSharing()
    {
      if (_simulation == default) return;

      try
      {
        _simulation.SavePrivateData(
          new _SimSharedStateDTO
          {
            ParameterSharedStates = ParameterSharedStates
              .Map(pss => new _SimParameterSharedStateDTO
              {
                Name = pss.Name,
                Value = pss.Value,
                Minimum = pss.Minimum,
                Maximum = pss.Maximum,
                Distribution = pss.Distribution.MatchUnsafe<string?>(d => d.ToString(), () => default)
              })
              .ToArray(),

            ElementSharedStates = ElementSharedStates
              .Map(ess => ess.Name)
              .ToArray(),

            ObservationsSharedStates = ObservationsSharedStates
              .Map(oss => oss.Reference)
              .ToArray()
          },
          nameof(RVisUI),
          nameof(Model),
          nameof(SimSharedState)
          );
      }
      finally
      {
        ParameterSharedStates = default;
        ElementSharedStates = default;
        ObservationsSharedStates = default;
        _simulation = default;
      }
    }

    public Arr<SimParameterSharedState> ParameterSharedStates
    {
      get => _simParameterSharedStates;
      private set => this.RaiseAndSetIfChanged(ref _simParameterSharedStates, value, PropertyChanged);
    }
    private Arr<SimParameterSharedState> _simParameterSharedStates;

    public IObservable<(Arr<SimParameterSharedState> ParameterSharedStates, ObservableQualifier ObservableQualifier)> ParameterSharedStateChanges =>
      _parameterSharedStateChangesSubject.AsObservable();
    private readonly ISubject<(Arr<SimParameterSharedState> ParameterSharedStates, ObservableQualifier ObservableQualifier)> _parameterSharedStateChangesSubject =
      new Subject<(Arr<SimParameterSharedState> ParameterSharedStates, ObservableQualifier ObservableQualifier)>();

    public void ShareParameterState(Arr<(string Name, double Value, double Minimum, double Maximum, Option<IDistribution> Distribution)> state)
    {
      var parameterSharedStates = ParameterSharedStates;

      var shares = state
        .Map(s =>
        {
          var newState = new SimParameterSharedState(s.Name, s.Value, s.Minimum, s.Maximum, s.Distribution);

          var index = parameterSharedStates.FindIndex(pss => pss.Name == s.Name);

          if (index.IsFound())
          {
            var existing = parameterSharedStates[index];
            if (existing != newState)
            {
              parameterSharedStates = parameterSharedStates.SetItem(index, newState);
              return Some((State: newState, ObservableQualifier: ObservableQualifier.Change));
            }
          }
          else
          {
            parameterSharedStates = parameterSharedStates.Add(newState);
            return Some((State: newState, ObservableQualifier: ObservableQualifier.Add));
          }

          return None;
        })
        .Somes()
        .ToArr();

      if (shares.IsEmpty) return;

      ParameterSharedStates = parameterSharedStates;

      var changes = shares
        .Filter(s => s.ObservableQualifier.IsChange())
        .Map(s => s.State);

      if (!changes.IsEmpty)
      {
        _parameterSharedStateChangesSubject.OnNext((changes, ObservableQualifier.Change));
      }

      var additions = shares
        .Filter(s => s.ObservableQualifier.IsAdd())
        .Map(s => s.State);

      if (!additions.IsEmpty)
      {
        _parameterSharedStateChangesSubject.OnNext((additions, ObservableQualifier.Add));
      }
    }

    public void UnshareParameterState(Arr<string> names)
    {
      var parameterSharedStates = ParameterSharedStates;

      var unshared = names
        .Map(n =>
        {
          var index = parameterSharedStates.FindIndex(pss => pss.Name == n);

          if (index.IsFound())
          {
            var oldState = parameterSharedStates[index];
            parameterSharedStates = parameterSharedStates.RemoveAt(index);
            return Some(oldState);
          }

          return None;
        })
        .Somes()
        .ToArr();

      if (unshared.IsEmpty) return;

      ParameterSharedStates = parameterSharedStates;

      _parameterSharedStateChangesSubject.OnNext((unshared, ObservableQualifier.Remove));
    }

    public Arr<SimElementSharedState> ElementSharedStates
    {
      get => _simElementSharedStates;
      private set => this.RaiseAndSetIfChanged(ref _simElementSharedStates, value, PropertyChanged);
    }
    private Arr<SimElementSharedState> _simElementSharedStates;

    public IObservable<(Arr<SimElementSharedState> ElementSharedStates, ObservableQualifier ObservableQualifier)> ElementSharedStateChanges =>
      _elementSharedStateChangesSubject.AsObservable();
    private readonly ISubject<(Arr<SimElementSharedState> ElementSharedStates, ObservableQualifier ObservableQualifier)> _elementSharedStateChangesSubject =
      new Subject<(Arr<SimElementSharedState> ElementSharedStates, ObservableQualifier ObservableQualifier)>();

    public void ShareElementState(Arr<string> names)
    {
      var elementSharedStates = ElementSharedStates;

      var shares = names
        .Map(n =>
        {
          var newState = new SimElementSharedState(n);

          var index = elementSharedStates.FindIndex(ess => ess.Name == n);

          if (index.IsFound())
          {
            var existing = elementSharedStates[index];
            if (existing != newState)
            {
              elementSharedStates = elementSharedStates.SetItem(index, newState);
              return Some((State: newState, ObservableQualifier: ObservableQualifier.Change));
            }
          }
          else
          {
            elementSharedStates = elementSharedStates.Add(newState);
            return Some((State: newState, ObservableQualifier: ObservableQualifier.Add));
          }

          return None;
        })
        .Somes()
        .ToArr();

      if (shares.IsEmpty) return;

      ElementSharedStates = elementSharedStates;

      var changes = shares
        .Filter(s => s.ObservableQualifier.IsChange())
        .Map(s => s.State);

      if (!changes.IsEmpty)
      {
        _elementSharedStateChangesSubject.OnNext((changes, ObservableQualifier.Change));
      }

      var additions = shares
        .Filter(s => s.ObservableQualifier.IsAdd())
        .Map(s => s.State);

      if (!additions.IsEmpty)
      {
        _elementSharedStateChangesSubject.OnNext((additions, ObservableQualifier.Add));
      }
    }

    public void UnshareElementState(Arr<string> names)
    {
      var elementSharedStates = ElementSharedStates;

      var unshared = names
        .Map(n =>
        {
          var index = elementSharedStates.FindIndex(ess => ess.Name == n);

          if (index.IsFound())
          {
            var oldState = elementSharedStates[index];
            elementSharedStates = elementSharedStates.RemoveAt(index);
            return Some(oldState);
          }

          return None;
        })
        .Somes()
        .ToArr();

      if (unshared.IsEmpty) return;

      ElementSharedStates = elementSharedStates;

      _elementSharedStateChangesSubject.OnNext((unshared, ObservableQualifier.Remove));
    }

    public Arr<SimObservationsSharedState> ObservationsSharedStates
    {
      get => _simObservationsSharedStates;
      private set => this.RaiseAndSetIfChanged(ref _simObservationsSharedStates, value, PropertyChanged);
    }
    private Arr<SimObservationsSharedState> _simObservationsSharedStates;

    public IObservable<(Arr<SimObservationsSharedState> ObservationsSharedStates, ObservableQualifier ObservableQualifier)> ObservationsSharedStateChanges =>
      _observationsSharedStateChangesSubject.AsObservable();
    private readonly ISubject<(Arr<SimObservationsSharedState> ObservationsSharedStates, ObservableQualifier ObservableQualifier)> _observationsSharedStateChangesSubject =
      new Subject<(Arr<SimObservationsSharedState> ObservationsSharedStates, ObservableQualifier ObservableQualifier)>();

    public void ShareObservationsState(Arr<string> references)
    {
      var observationsSharedStates = ObservationsSharedStates;

      var shares = references
        .Map(r =>
        {
          var newState = new SimObservationsSharedState(r);

          var index = observationsSharedStates.FindIndex(oss => oss.Reference == r);

          if (index.IsFound())
          {
            var existing = observationsSharedStates[index];
            if (existing != newState)
            {
              observationsSharedStates = observationsSharedStates.SetItem(index, newState);
              return Some((State: newState, ObservableQualifier: ObservableQualifier.Change));
            }
          }
          else
          {
            observationsSharedStates = observationsSharedStates.Add(newState);
            return Some((State: newState, ObservableQualifier: ObservableQualifier.Add));
          }

          return None;
        })
        .Somes()
        .ToArr();

      if (shares.IsEmpty) return;

      ObservationsSharedStates = observationsSharedStates;

      var changes = shares
        .Filter(s => s.ObservableQualifier.IsChange())
        .Map(s => s.State);

      if (!changes.IsEmpty)
      {
        _observationsSharedStateChangesSubject.OnNext((changes, ObservableQualifier.Change));
      }

      var additions = shares
        .Filter(s => s.ObservableQualifier.IsAdd())
        .Map(s => s.State);

      if (!additions.IsEmpty)
      {
        _observationsSharedStateChangesSubject.OnNext((additions, ObservableQualifier.Add));
      }
    }

    public void UnshareObservationsState(Arr<string> references)
    {
      var observationsSharedStates = ObservationsSharedStates;

      var unshared = references
        .Map(r =>
        {
          var index = observationsSharedStates.FindIndex(oss => oss.Reference == r);

          if (index.IsFound())
          {
            var oldState = observationsSharedStates[index];
            observationsSharedStates = observationsSharedStates.RemoveAt(index);
            return Some(oldState);
          }

          return None;
        })
        .Somes()
        .ToArr();

      if (unshared.IsEmpty) return;

      ObservationsSharedStates = observationsSharedStates;

      _observationsSharedStateChangesSubject.OnNext((unshared, ObservableQualifier.Remove));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Dispose() => Dispose(true);

    private void Dispose(bool disposing)
    {
      if (!_disposed)
      {
        if (disposing)
        {
          try
          {
            StopSharing();
          }
          catch (Exception) { }
        }

        _disposed = true;
      }
    }

    internal void SetState(Arr<SimParameterSharedState> state)
    {
      RequireUniqueElements(state, s => s.Name);

      var added = state.Filter(s => !ParameterSharedStates.ContainsState(s.Name));
      var removed = ParameterSharedStates.Filter(pss => !state.ContainsState(pss.Name));
      var changed = state
        .Map(s =>
          ParameterSharedStates
            .FindState(s)
            .Match(pss => Some((New: s, Existing: pss)), () => None)
         )
        .Somes()
        .Where(t => t.New != t.Existing)
        .Select(t => t.New)
        .ToArr();

      ParameterSharedStates = state;

      if (!added.IsEmpty)
      {
        _parameterSharedStateChangesSubject.OnNext((added, ObservableQualifier.Add));
      }

      if (!removed.IsEmpty)
      {
        _parameterSharedStateChangesSubject.OnNext((removed, ObservableQualifier.Remove));
      }

      if (!changed.IsEmpty)
      {
        _parameterSharedStateChangesSubject.OnNext((changed, ObservableQualifier.Change));
      }
    }

    internal void SetState(Arr<SimElementSharedState> state)
    {
      RequireUniqueElements(state, s => s.Name);

      var added = state.Filter(s => !ElementSharedStates.ContainsState(s.Name));
      var removed = ElementSharedStates.Filter(ess => !state.ContainsState(ess.Name));
      var changed = state
        .Map(s =>
          ElementSharedStates
            .FindState(s)
            .Match(ess => Some((New: s, Existing: ess)), () => None)
         )
        .Somes()
        .Where(t => t.New != t.Existing)
        .Select(t => t.New)
        .ToArr();

      ElementSharedStates = state;

      if (!added.IsEmpty)
      {
        _elementSharedStateChangesSubject.OnNext((added, ObservableQualifier.Add));
      }

      if (!removed.IsEmpty)
      {
        _elementSharedStateChangesSubject.OnNext((removed, ObservableQualifier.Remove));
      }

      if (!changed.IsEmpty) // never!
      {
        _elementSharedStateChangesSubject.OnNext((changed, ObservableQualifier.Change));
      }
    }

    internal void SetState(Arr<SimObservationsSharedState> state)
    {
      RequireUniqueElements(state, s => s.Reference);

      var added = state.Filter(s => !ObservationsSharedStates.ContainsState(s.Reference));
      var removed = ObservationsSharedStates.Filter(oss => !state.ContainsState(oss.Reference));
      var changed = state
        .Map(s =>
          ObservationsSharedStates
            .FindState(s)
            .Match(ess => Some((New: s, Existing: ess)), () => None)
         )
        .Somes()
        .Where(t => t.New != t.Existing)
        .Select(t => t.New)
        .ToArr();

      ObservationsSharedStates = state;

      if (!added.IsEmpty)
      {
        _observationsSharedStateChangesSubject.OnNext((added, ObservableQualifier.Add));
      }

      if (!removed.IsEmpty)
      {
        _observationsSharedStateChangesSubject.OnNext((removed, ObservableQualifier.Remove));
      }

      if (!changed.IsEmpty) // never!
      {
        _observationsSharedStateChangesSubject.OnNext((changed, ObservableQualifier.Change));
      }
    }

    private Simulation _simulation;
    private bool _disposed = false;
  }
}
