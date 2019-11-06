using LanguageExt;
using RVis.Model;
using RVis.Model.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using static LanguageExt.Prelude;
using static RVis.Base.Check;

namespace Plot
{
  internal sealed class OutputGroupStore : IDisposable
  {
    internal OutputGroupStore(Simulation simulation)
    {
      _pathToStoreDirectory = simulation.GetPrivateDirectory(
        nameof(Plot),
        nameof(ViewModel),
        nameof(OutputGroupStore)
        );

      var pathToStore = Path.Combine(_pathToStoreDirectory, _storeFileName);

      if (File.Exists(pathToStore))
      {
        var lines = File.ReadAllLines(pathToStore);
        var defaultInput = simulation.SimConfig.SimInput;

        var outputGroups = lines
          .Map(l => OutputGroup.Parse(l, defaultInput))
          .Somes();

        _outputGroups.AddRange(outputGroups);
      }
    }

    internal IObservable<(OutputGroup OutputGroup, bool Added)> ActivatedOutputGroups =>
      _activatedOutputGroupsSubject.AsObservable();

    internal Option<OutputGroup> ActiveOutputGroup => _outputGroups.Count == 0 ? None : Some(_outputGroups[0]);

    internal IReadOnlyList<OutputGroup> OutputGroups
    {
      get
      {
        lock (_syncLock) { return _outputGroups.AsReadOnly(); }
      }
    }

    internal void AddAndActivate(OutputGroup outputGroup)
    {
      lock (_syncLock)
      {
        RequireFalse(_outputGroups.Contains(outputGroup));
        _outputGroups.Insert(0, outputGroup);
        _activatedOutputGroupsSubject.OnNext((outputGroup, true));
      }
    }

    internal void Activate(OutputGroup outputGroup)
    {
      lock (_syncLock)
      {
        RequireTrue(_outputGroups.Contains(outputGroup));
        if (_outputGroups[0] != outputGroup)
        {
          _outputGroups.Remove(outputGroup);
          _outputGroups.Insert(0, outputGroup);
        }
        _activatedOutputGroupsSubject.OnNext((outputGroup, false));
      }
    }

    internal bool IsActiveGroupSeries(Arr<SimInput> serieInputs)
    {
      return ActiveOutputGroup
        .Match(og =>
          {
            var serieInputHashes = serieInputs.Map(i => i.Hash).OrderBy(h => h);
            var outputGroupHashes = og.SerieInputHashes.OrderBy(h => h);
            return serieInputHashes.SequenceEqual(outputGroupHashes);
          },
          () => false
        );
    }

    internal void Save()
    {
      if (!Directory.Exists(_pathToStoreDirectory)) Directory.CreateDirectory(_pathToStoreDirectory);
      var pathToStore = Path.Combine(_pathToStoreDirectory, _storeFileName);
      IEnumerable<string> lines;
      lock (_syncLock)
      {
        lines = _outputGroups.Map(og => og.ToString());
      }
      File.WriteAllLines(pathToStore, lines);
    }

    public void Dispose() => Dispose(true);

    private void Dispose(bool disposing)
    {
      if (!_disposed)
      {
        if (disposing)
        {
          if (_activatedOutputGroupsSubject is IDisposable activatedOutputGroupsSubject)
          {
            activatedOutputGroupsSubject.Dispose();
          }
        }

        _disposed = true;
      }
    }

    private const string _storeFileName = "outputgroups.txt";
    private readonly ISubject<(OutputGroup OutputGroup, bool Added)> _activatedOutputGroupsSubject =
      new Subject<(OutputGroup OutputGroup, bool Added)>();
    private readonly string _pathToStoreDirectory;
    private readonly List<OutputGroup> _outputGroups = new List<OutputGroup>();
    private readonly object _syncLock = new object();
    private bool _disposed = false;
  }
}
