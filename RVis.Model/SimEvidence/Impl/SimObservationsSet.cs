using LanguageExt;
using RVis.Base.Extensions;
using System;
using System.Linq;
using static LanguageExt.Prelude;
using static RVis.Base.Check;
using static RVis.Base.Extensions.NumExt;
using static System.Environment;
using static System.IO.File;
using static System.IO.Path;
using static System.String;

namespace RVis.Model
{
  public readonly partial struct SimObservationsSet
  {
    internal static SimObservationsSet LoadOrCreate(string pathToEvidenceDirectory, string subject)
    {
      var pathToCsvFile = Combine(pathToEvidenceDirectory, subject + ".csv");
      if (!Exists(pathToCsvFile))
      {
        return new SimObservationsSet(subject, Arr<SimObservations>.Empty);
      }

      var lines = ReadAllLines(pathToCsvFile);
      if (lines.IsEmpty())
      {
        return new SimObservationsSet(subject, Arr<SimObservations>.Empty);
      }

      var id = NOT_FOUND;
      var evidenceSourceID = NOT_FOUND;
      string? refName = default;

      var triples = lines
        .Skip(1) // header
        .Select(l =>
        {
          var parts = l.Split(',').Select(p => p.Trim()).ToArray();

          RequireTrue(
            parts.Length > 1,
            $"Invalid evidence CSV in {pathToCsvFile}: {l}"
            );
          RequireFalse(
            parts.Length == 2 && (id.IsntFound() || evidenceSourceID.IsntFound()),
            $"Invalid evidence CSV in {pathToCsvFile}: {l}"
            );
          RequireTrue(
            double.TryParse(parts[0], out double x),
            $"Invalid X in {pathToCsvFile}: {parts[0]}"
            );
          RequireTrue(
            double.TryParse(parts[1], out double y),
            $"Invalid {subject} in {pathToCsvFile}: {parts[1]}"
            );

          if (parts.Length > 2)
          {
            RequireTrue(
              parts.Length == 5,
              $"Invalid evidence CSV in {pathToCsvFile}: {l}"
              );
            RequireTrue(
              int.TryParse(parts[2], out id),
              $"Invalid ID in {pathToCsvFile}: {parts[2]}"
              );
            RequireTrue(
              int.TryParse(parts[3], out evidenceSourceID),
              $"Invalid evidence source ID in {pathToCsvFile}: {parts[3]}"
              );

            refName = parts[4].Trim('"').Trim();
            RequireTrue(refName.IsAString());
          }

          return (X: x, Y: y, RefName: refName, EvidenceSourceID: evidenceSourceID, ID: id);
        });

      var observations = triples
        .GroupBy(t => t.ID)
        .Select(g => new SimObservations(
          g.Key,
          g.First().EvidenceSourceID,
          subject,
          g.First().RefName ?? throw new Exception("null evidence source RefName"),
          g.Select(t => t.X).ToArr(),
          g.Select(t => t.Y).ToArr()
          ))
        .ToArr();

      RequireTrue(
        observations.IsEmpty || observations.AllUnique(o => o.ID),
        "Found duplicate IDs"
        );
      RequireTrue(
        observations.IsEmpty || observations.AllUnique(o => (o.EvidenceSourceID, o.RefName)),
        "Found duplicate RefNames"
        );

      return new SimObservationsSet(subject, observations);
    }

    internal static void Save(SimObservationsSet observationsSet, string pathToEvidenceDirectory)
    {
      RequireDirectory(pathToEvidenceDirectory);

      var pathToCsvFile = Combine(pathToEvidenceDirectory, observationsSet.Subject + ".csv");
      if (Exists(pathToCsvFile)) Delete(pathToCsvFile);

      static string ObservationToLine(SimObservations observations, int row)
      {
        var s = $"{observations.X[row]},{observations.Y[row]}";
        if (0 == row)
        {
          s = $"{s},{observations.ID},{observations.EvidenceSourceID},{observations.RefName.ToCsvQuoted()}";
        }
        return s;
      }

      var lines = observationsSet.Observations
        .Map(o => Range(0, o.X.Count).Select(i => ObservationToLine(o, i)).ToArr())
        .Bind(ss => ss);

      var csv =
        $"x,{observationsSet.Subject.Replace(',', '_')},oid,esid,ref" + NewLine +
        Join(NewLine, lines);

      WriteAllText(pathToCsvFile, csv);
    }

    internal static (SimObservationsSet ObservationsSet, SimObservations Observations) AddObservations(
      SimObservationsSet observationsSet,
      int evidenceSourceID,
      string refName,
      Arr<double> x,
      Arr<double> y
      )
    {
      RequireFalse(observationsSet.Observations.Exists(
        o => o.EvidenceSourceID == evidenceSourceID && o.RefName == refName)
        );

      var id = observationsSet.Observations.IsEmpty
        ? 1
        : observationsSet.Observations.Max(o => o.ID) + 1;

      var observations = new SimObservations(
        id,
        evidenceSourceID,
        observationsSet.Subject,
        refName,
        x,
        y
        );

      observationsSet = new SimObservationsSet(
        observationsSet.Subject,
        observationsSet.Observations.Add(observations)
        );

      return (observationsSet, observations);
    }

    internal static SimObservationsSet RemoveAllObservations(
      SimObservationsSet observationsSet,
      int evidenceSourceID
      )
    {
      var observations = observationsSet.Observations.Filter(
        o => o.EvidenceSourceID != evidenceSourceID
        );
      return new SimObservationsSet(observationsSet.Subject, observations);
    }

    internal static SimObservationsSet RemoveObservations(
      SimObservationsSet observationsSet,
      SimObservations toRemove
      )
    {
      var observations = observationsSet.Observations.Filter(
        o => o.ID != toRemove.ID
        );
      return new SimObservationsSet(observationsSet.Subject, observations);
    }
  }
}
