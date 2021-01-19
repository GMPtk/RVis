using System;
using System.Collections.Generic;

namespace RVisUI.Model
{
  public readonly struct SimObservationsSharedState : IEquatable<SimObservationsSharedState>
  {
    public SimObservationsSharedState(string reference) =>
      Reference = reference;

    public string Reference { get; }

    public override bool Equals(object? obj) => 
      obj is SimObservationsSharedState observationsSharedState && Equals(observationsSharedState);

    public bool Equals(SimObservationsSharedState other) => 
      Reference == other.Reference;

    public override int GetHashCode() => 
      -1304721846 + EqualityComparer<string>.Default.GetHashCode(Reference);

    public static bool operator ==(SimObservationsSharedState lhs, SimObservationsSharedState rhs) => 
      lhs.Equals(rhs);

    public static bool operator !=(SimObservationsSharedState lhs, SimObservationsSharedState rhs) => 
      !(lhs == rhs);
  }
}
