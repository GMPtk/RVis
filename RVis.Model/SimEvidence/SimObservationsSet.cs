using LanguageExt;
using System;
using static RVis.Base.Check;

namespace RVis.Model
{
  public readonly partial struct SimObservationsSet : IEquatable<SimObservationsSet>
  {
    internal SimObservationsSet(string subject, Arr<SimObservations> observations)
    {
      RequireTrue(observations.ForAll(o => o.Subject == subject));

      Subject = subject;
      Observations = observations;
    }

    public string Subject { get; }

    public Arr<SimObservations> Observations { get; }

    public override bool Equals(object? obj) =>
      obj is SimObservationsSet observationsSet && Equals(observationsSet);

    public bool Equals(SimObservationsSet other) =>
      (Subject, Observations) == (other.Subject, other.Observations);

    public override int GetHashCode() =>
      HashCode.Combine(Subject, Observations);

    public static bool operator ==(SimObservationsSet lhs, SimObservationsSet rhs) =>
      lhs.Equals(rhs);

    public static bool operator !=(SimObservationsSet lhs, SimObservationsSet rhs) =>
      !(lhs == rhs);
  }
}
