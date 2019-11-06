using LanguageExt;
using RVis.Base;
using RVis.Base.Extensions;
using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using static RVis.Base.Check;
using static Sampling.Logger;
using static Sampling.SamplingDesign;
using static System.IO.Path;

namespace Sampling
{
  internal sealed class SamplingDesigns
  {
    internal SamplingDesigns(string pathToSamplingDesignsDirectory)
    {
      _pathToSamplingDesignsDirectory = pathToSamplingDesignsDirectory;
      Directory.CreateDirectory(_pathToSamplingDesignsDirectory);

      LoadDesignDigests();
    }

    internal Arr<DesignDigest> DesignDigests { get; private set; }

    internal IObservable<(DesignDigest DesignDigest, ObservableQualifier ObservableQualifier)> SamplingDesignChanges =>
      _samplingDesignChangesSubject.AsObservable();
    private readonly ISubject<(DesignDigest DesignDigest, ObservableQualifier ObservableQualifier)> _samplingDesignChangesSubject =
      new Subject<(DesignDigest DesignDigest, ObservableQualifier ObservableQualifier)>();

    internal void Add(SamplingDesign samplingDesign)
    {
      SaveSamplingDesign(samplingDesign, _pathToSamplingDesignsDirectory);

      var designDigest = new DesignDigest(samplingDesign.CreatedOn, samplingDesign.ToString());
      DesignDigests = designDigest + DesignDigests;
      SaveDesignDigests();

      _samplingDesignChangesSubject.OnNext((designDigest, ObservableQualifier.Add));
    }

    internal bool Update(SamplingDesign samplingDesign)
    {
      var index = DesignDigests.FindIndex(dd => dd.CreatedOn == samplingDesign.CreatedOn);

      if (index.IsntFound()) return false;

      UpdateSamplingDesign(samplingDesign, _pathToSamplingDesignsDirectory);

      var designDigest = new DesignDigest(samplingDesign.CreatedOn, samplingDesign.ToString());
      DesignDigests = DesignDigests.SetItem(index, designDigest);
      SaveDesignDigests();

      _samplingDesignChangesSubject.OnNext((designDigest, ObservableQualifier.Change));

      return true;
    }

    internal SamplingDesign Load(DateTime createdOn) =>
      LoadSamplingDesign(_pathToSamplingDesignsDirectory, createdOn);

    internal bool Remove(DateTime createdOn)
    {
      var index = DesignDigests.FindIndex(dd => dd.CreatedOn == createdOn);

      if (index.IsntFound()) return false;

      RemoveSamplingDesign(_pathToSamplingDesignsDirectory, createdOn);

      var designDigest = DesignDigests[index];
      DesignDigests = DesignDigests.RemoveAt(index);
      SaveDesignDigests();

      _samplingDesignChangesSubject.OnNext((designDigest, ObservableQualifier.Remove));

      return true;
    }

    private void SaveDesignDigests()
    {
      var lines = DesignDigests.Map(
        dd => dd.CreatedOn.ToDirectoryName() + "$" + dd.Description
        );

      var pathToDigestFile = Combine(_pathToSamplingDesignsDirectory, DIGEST_FILE_NAME);

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
      var pathToDigestFile = Combine(_pathToSamplingDesignsDirectory, DIGEST_FILE_NAME);

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
    private readonly string _pathToSamplingDesignsDirectory;
  }
}
