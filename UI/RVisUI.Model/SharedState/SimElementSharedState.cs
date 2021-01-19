using System;
using System.Collections.Generic;

namespace RVisUI.Model
{
  public readonly struct SimElementSharedState : IEquatable<SimElementSharedState>
  {
    public SimElementSharedState(string name) =>
      Name = name;

    public string Name { get; }

    public override bool Equals(object? obj) => 
      obj is SimElementSharedState elementSharedState && Equals(elementSharedState);

    public bool Equals(SimElementSharedState other) => 
      Name == other.Name;

    public override int GetHashCode() => 
      539060726 + EqualityComparer<string>.Default.GetHashCode(Name);

    public static bool operator ==(SimElementSharedState lhs, SimElementSharedState rhs) => 
      lhs.Equals(rhs);

    public static bool operator !=(SimElementSharedState lhs, SimElementSharedState rhs) => 
      !(lhs == rhs);
  }
}
