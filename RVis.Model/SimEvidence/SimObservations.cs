using LanguageExt;
using RVis.Base.Extensions;
using System;
using static RVis.Base.Check;

namespace RVis.Model
{
  public readonly struct SimObservations : IEquatable<SimObservations>
  {
    internal SimObservations(
      int id,
      int evidenceSourceID,
      string subject,
      string refName,
      Arr<double> x,
      Arr<double> y
      )
    {
      RequireTrue(id > 0);
      RequireTrue(evidenceSourceID > 0);
      RequireTrue(subject.IsAString());
      RequireTrue(refName.IsAString());
      RequireFalse(x.IsEmpty);
      RequireFalse(y.IsEmpty);
      RequireTrue(x.Count == y.Count);

      ID = id;
      EvidenceSourceID = evidenceSourceID;
      Subject = subject;
      RefName = refName;
      X = x;
      Y = y;
    }

    public int ID { get; }

    public int EvidenceSourceID { get; }

    public string Subject { get; }

    public string RefName { get; }

    public Arr<double> X { get; }

    public Arr<double> Y { get; }

    public override bool Equals(object? obj) =>
      obj is SimObservations observations && Equals(observations);

    public bool Equals(SimObservations other) =>
      (ID, EvidenceSourceID, Subject, RefName, X, Y) ==
      (other.ID, other.EvidenceSourceID, other.Subject, other.RefName, other.X, other.Y);

    public override int GetHashCode() =>
      HashCode.Combine(ID, EvidenceSourceID, Subject, RefName, X, Y);

    public static bool operator ==(SimObservations lhs, SimObservations rhs) =>
      lhs.Equals(rhs);

    public static bool operator !=(SimObservations lhs, SimObservations rhs) =>
      !(lhs == rhs);
  }
}
