using LanguageExt;
using RVis.Base;
using System;

namespace RVis.Model
{
  public interface ISimEvidence
  {
    Arr<SimEvidenceSource> EvidenceSources { get; }
    SimEvidenceSource AddEvidenceSource(
      string name, 
      string? description, 
      Set<string> subjects, 
      string reference, 
      string refHash
      );
    void RemoveEvidenceSource(int id);
    IObservable<(SimEvidenceSource EvidenceSource, ObservableQualifier Change)> EvidenceSourcesChanges { get; }

    Arr<SimObservations> AddObservations(
      int evidenceSourceID,
      Arr<(string Subject, string RefName, Arr<double> X, Arr<double> Y)> observationsSets
      );
    void RemoveObservations(int evidenceSourceID, string subject);
    void RemoveObservations(SimObservations observations);
    IObservable<(Arr<SimObservations> Observations, ObservableQualifier Change)> ObservationsChanges { get; }

    Arr<SimObservations> GetObservations(int evidenceSourceID);
    SimObservationsSet GetObservationSet(string subject);

    Set<string> Subjects { get; }
  }
}
