using LanguageExt;
using RVis.Base;
using RVis.Base.Extensions;
using RVis.Model.Extensions;
using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using static LanguageExt.Prelude;
using static Nett.Toml;
using static RVis.Base.Check;
using static RVis.Model.Constant;
using static System.IO.Path;

namespace RVis.Model
{
  public sealed class SimEvidence : ISimEvidence, IDisposable
  {
    public SimEvidence()
    {
    }

    public Arr<SimEvidenceSource> EvidenceSources { get; private set; }

    public SimEvidenceSource AddEvidenceSource(
      string name,
      string? description,
      Set<string> subjects,
      string refName,
      string refHash
      )
    {
      lock (_syncLock)
      {
        RequireFalse(
          EvidenceSources.ContainsEvidenceSource(refHash),
          $"Duplicate evidence source: {refHash}"
        );
        RequireTrue(subjects.ForAll(IsSubject));
        RequireNotNullEmptyWhiteSpace(_pathToEvidenceDirectory);

        var id = EvidenceSources.IsEmpty ? 1 : 1 + EvidenceSources.Max(es => es.ID);

        var evidenceSource = new SimEvidenceSource(id, name, description, subjects, refName, refHash);

        var evidenceSources = EvidenceSources.Add(evidenceSource);
        Save(evidenceSources, _pathToEvidenceDirectory);
        EvidenceSources = evidenceSources;

        _evidenceSourcesChangesSubject.OnNext((evidenceSource, ObservableQualifier.Add));

        return evidenceSource;
      }
    }

    public void RemoveEvidenceSource(int id)
    {
      RequireNotNullEmptyWhiteSpace(_pathToEvidenceDirectory);

      lock (_syncLock)
      {
        var index = EvidenceSources.FindIndex(es => es.ID == id);
        RequireTrue(index.IsFound(), $"Unknown evidence source: {id}");

        var evidenceSource = EvidenceSources[index];

        var observations = evidenceSource.Subjects
          .Map(s => RemoveObservationsImpl(id, s))
          .Bind(a => a)
          .ToArr();

        _observationsChangesSubject.OnNext((observations, ObservableQualifier.Remove));

        var evidenceSources = EvidenceSources.RemoveAt(index);
        Save(evidenceSources, _pathToEvidenceDirectory);
        EvidenceSources = evidenceSources;

        _evidenceSourcesChangesSubject.OnNext((evidenceSource, ObservableQualifier.Remove));
      }
    }

    public IObservable<(SimEvidenceSource EvidenceSource, ObservableQualifier Change)> EvidenceSourcesChanges =>
      _evidenceSourcesChangesSubject.AsObservable();

    public Arr<SimObservations> AddObservations(
      int evidenceSourceID,
      Arr<(string Subject, string RefName, Arr<double> X, Arr<double> Y)> additions
      )
    {
      RequireNotNullEmptyWhiteSpace(_pathToEvidenceDirectory);

      lock (_syncLock)
      {
        var esIndex = EvidenceSources.FindIndex(es => es.ID == evidenceSourceID);
        RequireTrue(esIndex.IsFound(), $"Unknown evidence source: {evidenceSourceID}");

        var evidenceSource = EvidenceSources[esIndex];

        RequireTrue(additions
          .Map(a => a.Subject)
          .ForAll(s => IsSubject(s) && evidenceSource.Subjects.Contains(s)),
          "Subjects mismatch in additions"
          );

        var edits = additions
          .GroupBy(a => a.Subject)
          .Select(g =>
          {
            var observationsSet = FindOrLoadOrCreateObservationsSet(g.Key);

            var addedObservations = g
              .Select(a =>
              {
                var (addedTo, added) = SimObservationsSet.AddObservations(
                  observationsSet,
                  evidenceSourceID,
                  a.RefName,
                  a.X,
                  a.Y
                  );
                observationsSet = addedTo;
                return added;
              })
              .ToArr();

            return (ObservationsSet: observationsSet, AddedObservations: addedObservations);
          })
          .ToArr();

        var observationsSets = _observationsSets;

        void SaveObservationsSet(SimObservationsSet observationsSet)
        {
          SimObservationsSet.Save(observationsSet, _pathToEvidenceDirectory);
          var osIndex = observationsSets.FindIndex(os => os.Subject == observationsSet.Subject);
          observationsSets = osIndex.IsFound()
            ? observationsSets.SetItem(osIndex, observationsSet)
            : observationsSets.Add(observationsSet);
        }

        edits.Map(e => e.ObservationsSet).Iter(SaveObservationsSet);

        _observationsSets = observationsSets;

        var observations = edits.Bind(e => e.AddedObservations);

        _observationsChangesSubject.OnNext((observations, ObservableQualifier.Add));

        return observations;
      }
    }

    public void RemoveObservations(int evidenceSourceID, string subject)
    {
      lock (_syncLock)
      {
        RequireTrue(IsSubject(subject));

        var removed = RemoveObservationsImpl(evidenceSourceID, subject);

        _observationsChangesSubject.OnNext((removed, ObservableQualifier.Remove));
      }
    }

    public void RemoveObservations(SimObservations observations)
    {
      RequireNotNullEmptyWhiteSpace(_pathToEvidenceDirectory);

      lock (_syncLock)
      {
        var observationsSet = FindOrLoadOrCreateObservationsSet(observations.Subject);
        observationsSet = SimObservationsSet.RemoveObservations(observationsSet, observations);
        SimObservationsSet.Save(observationsSet, _pathToEvidenceDirectory);
        var index = _observationsSets.FindIndex(os => os.Subject == observationsSet.Subject);
        var observationsSets = index.IsFound()
          ? _observationsSets.SetItem(index, observationsSet)
          : _observationsSets.Add(observationsSet);
        _observationsChangesSubject.OnNext((Array(observations), ObservableQualifier.Remove));
      }
    }

    public IObservable<(Arr<SimObservations> Observations, ObservableQualifier Change)> ObservationsChanges =>
      _observationsChangesSubject.AsObservable();

    public Arr<SimObservations> GetObservations(int evidenceSourceID)
    {
      lock (_syncLock)
      {
        var index = EvidenceSources.FindIndex(es => es.ID == evidenceSourceID);
        RequireTrue(index.IsFound(), $"Unknown evidence source: {evidenceSourceID}");

        var observationsSets = EvidenceSources[index].Subjects
          .ToArr()
          .Map(s => FindOrLoadOrCreateObservationsSet(s));

        var missing = observationsSets.Filter(os => !_observationsSets.ContainsObservationsSet(os.Subject));

        _observationsSets = _observationsSets.AddRange(missing);

        return observationsSets.Bind(
          os => os.Observations.Filter(o => o.EvidenceSourceID == evidenceSourceID)
          );
      }
    }

    public SimObservationsSet GetObservationSet(string subject)
    {
      lock (_syncLock)
      {
        RequireTrue(IsSubject(subject));

        var observationsSet = FindOrLoadOrCreateObservationsSet(subject);

        if (!_observationsSets.ContainsObservationsSet(observationsSet.Subject))
        {
          _observationsSets = _observationsSets.Add(observationsSet);
        }

        return observationsSet;
      }
    }

    public Set<string> Subjects { get; private set; }

    public void Load(Simulation simulation)
    {
      lock (_syncLock)
      {
        RequireFalse(_pathToEvidenceDirectory.IsAString());
        RequireTrue(Subjects.IsEmpty);
        RequireTrue(EvidenceSources.IsEmpty);
        RequireTrue(_observationsSets.IsEmpty);

        var subjects = simulation.SimConfig.SimOutput.DependentVariables
          .Map(e => e.Name);

        Subjects = toSet(subjects);

        _pathToEvidenceDirectory = simulation.GetPrivateDirectory(EVIDENCE_SUBDIRECTORY);
        if (!Directory.Exists(_pathToEvidenceDirectory)) return;

        var pathToEvidenceSources = Combine(_pathToEvidenceDirectory, EVIDENCE_SOURCES_FILE_NAME);
        if (!File.Exists(pathToEvidenceSources)) return;

        var dto = ReadFile<EvidenceSourcesDTO>(pathToEvidenceSources);
        var evidenceSources = EvidenceSourcesDTO.FromDTO(dto);
        EvidenceSources = evidenceSources;
      }
    }

    public void Unload()
    {
      lock (_syncLock)
      {
        _pathToEvidenceDirectory = default;
        Subjects = default;
        EvidenceSources = default;
        _observationsSets = default;
      }
    }

    public void Dispose() => Dispose(true);

    private void Dispose(bool disposing)
    {
      if (!_disposed)
      {
        if (disposing)
        {
        }

        _disposed = true;
      }
    }

    private static void Save(Arr<SimEvidenceSource> evidenceSources, string pathToEvidenceDirectory)
    {
      var dto = EvidenceSourcesDTO.ToDTO(evidenceSources);
      var pathToEvidenceSources = Combine(pathToEvidenceDirectory, EVIDENCE_SOURCES_FILE_NAME);
      WriteFile(dto, pathToEvidenceSources);
    }

    private Arr<SimObservations> RemoveObservationsImpl(int evidenceSourceID, string subject)
    {
      var observationsSet = FindOrLoadOrCreateObservationsSet(subject);

      var removed = observationsSet.Observations.Filter(o => o.EvidenceSourceID == evidenceSourceID);

      if (!removed.IsEmpty)
      {
        RequireNotNullEmptyWhiteSpace(_pathToEvidenceDirectory);

        observationsSet = SimObservationsSet.RemoveAllObservations(observationsSet, evidenceSourceID);

        SimObservationsSet.Save(observationsSet, _pathToEvidenceDirectory);

        var index = _observationsSets.FindIndex(os => os.Subject == subject);

        var observationsSets = index.IsFound()
          ? _observationsSets.SetItem(index, observationsSet)
          : _observationsSets.Add(observationsSet);

        _observationsSets = observationsSets;
      }

      return removed;
    }

    private SimObservationsSet FindOrLoadOrCreateObservationsSet(string subject)
    {
      RequireNotNullEmptyWhiteSpace(_pathToEvidenceDirectory);

      return _observationsSets
        .FindObservationsSet(subject)
        .Match(
        os => os,
        () => SimObservationsSet.LoadOrCreate(_pathToEvidenceDirectory, subject)
        );
    }

    private bool IsSubject(string subject) =>
      SimEvidenceExt.IsSubject(this, subject);

    private string? _pathToEvidenceDirectory;
    private Arr<SimObservationsSet> _observationsSets;
    private readonly ISubject<(SimEvidenceSource SimEvidenceSource, ObservableQualifier Change)> _evidenceSourcesChangesSubject =
      new Subject<(SimEvidenceSource SimEvidenceSource, ObservableQualifier Change)>();
    private readonly ISubject<(Arr<SimObservations> SimObservations, ObservableQualifier Change)> _observationsChangesSubject =
      new Subject<(Arr<SimObservations> SimObservations, ObservableQualifier Change)>();
    private readonly object _syncLock = new object();
    private bool _disposed = false;
  }
}
