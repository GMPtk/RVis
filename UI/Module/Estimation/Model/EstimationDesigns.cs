using LanguageExt;
using RVis.Base;
using RVis.Base.Extensions;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using static RVis.Base.Check;
using static Estimation.Logger;
using static Estimation.EstimationDesign;
using static System.IO.Path;
using RVis.Model;

namespace Estimation
{
  internal sealed class EstimationDesigns
  {
    internal EstimationDesigns(string pathToEstimationDesignsDirectory, ISimEvidence evidence)
    {
      _pathToEstimationDesignsDirectory = pathToEstimationDesignsDirectory;
      _evidence = evidence;

      Directory.CreateDirectory(_pathToEstimationDesignsDirectory);

      LoadDesignDigests();
    }

    internal Arr<DesignDigest> DesignDigests { get; private set; }

    internal IObservable<(DesignDigest DesignDigest, ObservableQualifier ObservableQualifier)> EstimationDesignChanges =>
      _estimationDesignChangesSubject.AsObservable();
    private readonly ISubject<(DesignDigest DesignDigest, ObservableQualifier ObservableQualifier)> _estimationDesignChangesSubject =
      new Subject<(DesignDigest DesignDigest, ObservableQualifier ObservableQualifier)>();

    internal void Add(EstimationDesign estimationDesign)
    {
      SaveEstimationDesign(estimationDesign, _pathToEstimationDesignsDirectory, _evidence);

      var designDigest = new DesignDigest(estimationDesign.CreatedOn, estimationDesign.ToString());
      DesignDigests = designDigest + DesignDigests;
      SaveDesignDigests();

      _estimationDesignChangesSubject.OnNext((designDigest, ObservableQualifier.Add));
    }

    internal EstimationDesign Load(DateTime createdOn) =>
      LoadEstimationDesign(_pathToEstimationDesignsDirectory, createdOn, _evidence);

    internal bool Remove(DateTime createdOn)
    {
      var index = DesignDigests.FindIndex(dd => dd.CreatedOn == createdOn);

      if (index.IsntFound()) return false;

      RemoveEstimationDesign(_pathToEstimationDesignsDirectory, createdOn);

      var designDigest = DesignDigests[index];
      DesignDigests = DesignDigests.RemoveAt(index);
      SaveDesignDigests();

      _estimationDesignChangesSubject.OnNext((designDigest, ObservableQualifier.Remove));

      return true;
    }

    internal string GetPathToEstimationDesign(EstimationDesign estimationDesign) =>
      EstimationDesign.GetPathToEstimationDesign(estimationDesign, _pathToEstimationDesignsDirectory);

    internal bool Update(EstimationDesign estimationDesign)
    {
      var index = DesignDigests.FindIndex(dd => dd.CreatedOn == estimationDesign.CreatedOn);

      if (index.IsntFound()) return false;

      UpdateEstimationDesign(estimationDesign, _pathToEstimationDesignsDirectory, _evidence);

      var designDigest = new DesignDigest(estimationDesign.CreatedOn, estimationDesign.ToString());
      DesignDigests = DesignDigests.SetItem(index, designDigest);
      SaveDesignDigests();

      _estimationDesignChangesSubject.OnNext((designDigest, ObservableQualifier.Change));

      return true;
    }

    private void SaveDesignDigests()
    {
      var lines = DesignDigests.Map(
        dd => dd.CreatedOn.ToDirectoryName() + "$" + dd.Description
        );

      var pathToDigestFile = Combine(_pathToEstimationDesignsDirectory, DIGEST_FILE_NAME);

      try
      {
        File.WriteAllLines(pathToDigestFile, lines);
      }
      catch (Exception ex)
      {
        Log.Error(ex, $"Failed to save design digests to {pathToDigestFile}");
      }
    }

    private void LoadDesignDigests()
    {
      var pathToDigestFile = Combine(_pathToEstimationDesignsDirectory, DIGEST_FILE_NAME);

      if (!File.Exists(pathToDigestFile))
      {
        DesignDigests = default;
        return;
      }

      try
      {
        var lines = File.ReadAllLines(pathToDigestFile);

        DesignDigests = lines
          .Select(l =>
          {
            var parts = l.Split('$');
            RequireTrue(2 == parts.Length);
            var createdOn = parts[0].FromDirectoryName();
            var description = parts[1];
            return new DesignDigest(createdOn, description);
          })
          .ToArr();
      }
      catch (Exception ex)
      {
        Log.Error(ex, $"Failed to load design digests from {pathToDigestFile}");
      }
    }

    private const string DIGEST_FILE_NAME = "digest.txt";
    private readonly string _pathToEstimationDesignsDirectory;
    private readonly ISimEvidence _evidence;
  }
}
