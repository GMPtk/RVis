using LanguageExt;
using RVis.Base.Extensions;
using System;
using System.Diagnostics.CodeAnalysis;
using static LanguageExt.Prelude;
using static RVis.Base.Check;
using static RVis.Base.Extensions.LangExt;
using static RVis.Model.Logger;

namespace RVis.Model.Extensions
{
  public static class SimEvidenceExt
  {
    public static SimEvidenceSource GetEvidenceSource(this ISimEvidence evidence, int id) =>
      evidence.EvidenceSources
        .Find(es => es.ID == id)
        .AssertSome($"Unknown evidence source: {id}");

    public static string GetFQObservationsName(this SimObservations observations, SimEvidenceSource evidenceSource) =>
      observations.Subject != observations.RefName
        ? $"{observations.Subject} x {observations.X.Count} from {evidenceSource.Name} ({observations.RefName})"
        : $"{observations.Subject} x {observations.X.Count} from {evidenceSource.Name}";

    public static string GetFQObservationsName(this ISimEvidence evidence, SimObservations observations) =>
      observations.GetFQObservationsName(evidence.GetEvidenceSource(observations.EvidenceSourceID));

    public static bool ContainsObservations(this Arr<SimObservations> arr, SimObservations observations) =>
      arr.Exists(o => o.EvidenceSourceID == observations.EvidenceSourceID && o.ID == observations.ID);

    public static bool ContainsObservationsSet(this Arr<SimObservationsSet> observationsSets, string subject) =>
      observationsSets.Exists(os => os.Subject == subject);

    public static Option<SimObservationsSet> FindObservationsSet(this Arr<SimObservationsSet> observationsSets, string subject) =>
      observationsSets.Find(os => os.Subject == subject);

    public static bool ContainsEvidenceSource(this Arr<SimEvidenceSource> evidenceSources, string refHash) =>
      evidenceSources.Exists(es => es.RefHash == refHash);

    public static Option<SimEvidenceSource> FindEvidenceSource(this Arr<SimEvidenceSource> evidenceSources, string refHash) =>
      evidenceSources.Find(es => es.RefHash == refHash);

    public static Option<SimEvidenceSource> FindEvidenceSource(this Arr<SimEvidenceSource> evidenceSources, SimEvidenceSource evidenceSource) =>
      FindEvidenceSource(evidenceSources, evidenceSource.RefHash);

    public static bool IsSubject(this ISimEvidence evidence, [NotNullWhen(true)] string? subject) =>
      subject.IsAString() && evidence.Subjects.Contains(subject);

    public static string GetReference(this SimEvidenceSource evidenceSource) =>
      $"{evidenceSource.ID}/{evidenceSource.RefName}/{evidenceSource.RefHash}";

    public static string GetReference(this ISimEvidence evidence, SimObservations observations)
    {
      var evidenceSource = evidence.EvidenceSources
        .Find(es => es.ID == observations.EvidenceSourceID)
        .AssertSome();

      return $"{evidenceSource.GetReference()}/{observations.ID}/{observations.Subject}/{observations.RefName}";
    }

    public static Option<SimEvidenceSource> GetEvidenceSource(this ISimEvidence evidence, string reference)
    {
      try
      {
        var parts = reference.Split('/');
        RequireTrue(parts.Length == 3 || parts.Length == 6);
        RequireTrue(int.TryParse(parts[0], out int evidenceSourceID));
        var evidenceSourceRefName = parts[1];
        RequireTrue(evidenceSourceRefName.IsAString());
        var evidenceSourceRefHash = parts[2];
        RequireTrue(evidenceSourceRefHash.IsAString());

        return evidence.EvidenceSources
          .Find(es => es.ID == evidenceSourceID &&
                      es.RefName == evidenceSourceRefName &&
                      es.RefHash == evidenceSourceRefHash
                      );
      }
      catch (Exception ex)
      {
        Log.Error(ex, $"Invalid evidence source reference: {reference}");
        return None;
      }
    }

    public static Option<SimObservations> GetObservations(this ISimEvidence evidence, string reference)
    {
      try
      {
        var parts = reference.Split('/');
        RequireTrue(parts.Length == 6);

        Option<SimObservations> SomeEvidenceSource(SimEvidenceSource es)
        {
          RequireTrue(int.TryParse(parts[3], out int observationsID));
          var subject = parts[4];
          RequireTrue(subject.IsAString());
          var observationsRefName = parts[5];
          RequireTrue(observationsRefName.IsAString());

          var observationsSet = evidence.GetObservationSet(subject);
          return observationsSet.Observations.Find(
            o => o.ID == observationsID &&
                 o.RefName == observationsRefName &&
                 o.EvidenceSourceID == es.ID
            );
        }

        return evidence
          .GetEvidenceSource(reference)
          .Match(SomeEvidenceSource, NoneOf<SimObservations>);
      }
      catch (Exception ex)
      {
        Log.Error(ex, $"Invalid observations reference: {reference}");
        return None;
      }
    }
  }
}
