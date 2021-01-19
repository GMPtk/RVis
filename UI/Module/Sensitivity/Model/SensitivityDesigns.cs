using LanguageExt;
using RVis.Base;
using RVis.Base.Extensions;
using RVis.Data;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using static RVis.Base.Check;
using static Sensitivity.Logger;
using static Sensitivity.SensitivityDesign;
using static System.IO.Path;
using DataTable = System.Data.DataTable;

namespace Sensitivity
{
  internal sealed class SensitivityDesigns
  {
    internal SensitivityDesigns(string pathToSensitivityDesignsDirectory)
    {
      _pathToSensitivityDesignsDirectory = pathToSensitivityDesignsDirectory;
      Directory.CreateDirectory(_pathToSensitivityDesignsDirectory);

      LoadDesignDigests();
    }

    internal Arr<DesignDigest> DesignDigests { get; private set; }

    internal IObservable<(DesignDigest DesignDigest, ObservableQualifier ObservableQualifier)> SensitivityDesignChanges =>
      _sensitivityDesignChangesSubject.AsObservable();
    private readonly ISubject<(DesignDigest DesignDigest, ObservableQualifier ObservableQualifier)> _sensitivityDesignChangesSubject =
      new Subject<(DesignDigest DesignDigest, ObservableQualifier ObservableQualifier)>();

    internal void Add(SensitivityDesign sensitivityDesign)
    {
      SaveSensitivityDesign(sensitivityDesign, _pathToSensitivityDesignsDirectory);

      var designDigest = new DesignDigest(sensitivityDesign.CreatedOn, sensitivityDesign.ToString());
      DesignDigests = designDigest + DesignDigests;
      SaveDesignDigests();

      _sensitivityDesignChangesSubject.OnNext((designDigest, ObservableQualifier.Add));
    }

    internal void SaveTrace(SensitivityDesign sensitivityDesign, NumDataTable trace) => 
      SaveSensitivityDesignTrace(sensitivityDesign, trace, _pathToSensitivityDesignsDirectory);

    internal NumDataTable? LoadTrace(SensitivityDesign sensitivityDesign) => 
      LoadSensitivityDesignTrace(sensitivityDesign, _pathToSensitivityDesignsDirectory);

    internal void SaveRanking(SensitivityDesign sensitivityDesign, Ranking ranking) =>
      SaveSensitivityDesignRanking(sensitivityDesign, ranking, _pathToSensitivityDesignsDirectory);

    internal Ranking LoadRanking(SensitivityDesign sensitivityDesign) =>
      LoadSensitivityDesignRanking(sensitivityDesign, _pathToSensitivityDesignsDirectory);

    internal void SaveMorrisOutputMeasures(
      SensitivityDesign sensitivityDesign,
      string outputName,
      DataTable mu,
      DataTable muStar,
      DataTable sigma
      )
    {
      SensitivityDesign.SaveMorrisOutputMeasures(
        sensitivityDesign,
        outputName,
        mu,
        muStar,
        sigma,
        _pathToSensitivityDesignsDirectory
        );
    }

    internal void SaveFast99OutputMeasures(
      SensitivityDesign sensitivityDesign,
      string outputName,
      DataTable firstOrder,
      DataTable totalOrder,
      DataTable variance
      )
    {
      SensitivityDesign.SaveFast99OutputMeasures(
        sensitivityDesign, 
        outputName, 
        firstOrder, 
        totalOrder, 
        variance, 
        _pathToSensitivityDesignsDirectory
        );
    }

    internal bool LoadMorrisOutputMeasures(
      SensitivityDesign sensitivityDesign,
      string outputName,
      out (DataTable Mu, DataTable MuStar, DataTable Sigma) measures
      )
    {
      return SensitivityDesign.LoadMorrisOutputMeasures(
        sensitivityDesign,
        outputName,
        _pathToSensitivityDesignsDirectory,
        out measures
        );
    }

    internal bool LoadFast99OutputMeasures(
      SensitivityDesign sensitivityDesign,
      string outputName,
      out (DataTable FirstOrder, DataTable TotalOrder, DataTable Variance) measures
      )
    {
      return SensitivityDesign.LoadFast99OutputMeasures(
        sensitivityDesign,
        outputName,
        _pathToSensitivityDesignsDirectory,
        out measures
        );
    }

    internal SensitivityDesign Load(DateTime createdOn) =>
      LoadSensitivityDesign(_pathToSensitivityDesignsDirectory, createdOn);

    internal bool Remove(DateTime createdOn)
    {
      var index = DesignDigests.FindIndex(dd => dd.CreatedOn == createdOn);

      if (index.IsntFound()) return false;

      RemoveSensitivityDesign(_pathToSensitivityDesignsDirectory, createdOn);

      var designDigest = DesignDigests[index];
      DesignDigests = DesignDigests.RemoveAt(index);
      SaveDesignDigests();

      _sensitivityDesignChangesSubject.OnNext((designDigest, ObservableQualifier.Remove));

      return true;
    }

    private void SaveDesignDigests()
    {
      var lines = DesignDigests.Map(
        dd => dd.CreatedOn.ToDirectoryName() + "$" + dd.Description
        );

      var pathToDigestFile = Combine(_pathToSensitivityDesignsDirectory, DIGEST_FILE_NAME);

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
      var pathToDigestFile = Combine(_pathToSensitivityDesignsDirectory, DIGEST_FILE_NAME);

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
    private readonly string _pathToSensitivityDesignsDirectory;
  }
}
