using LanguageExt;
using RVis.Base.Extensions;
using System;
using static RVis.Base.Check;

namespace RVis.Model
{
  public readonly struct SimEvidenceSource : IEquatable<SimEvidenceSource>
  {
    internal SimEvidenceSource(
      int id,
      string name,
      string? description,
      Set<string> subjects,
      string refName,
      string refHash
      )
    {
      RequireTrue(id >= 0);
      RequireTrue(name.IsAString());
      RequireFalse(subjects.IsEmpty);
      RequireTrue(refName.IsAString());
      RequireTrue(refHash.IsAString());

      ID = id;
      Name = name;
      Description = description;
      Subjects = subjects;
      RefName = refName;
      RefHash = refHash;
    }

    public int ID { get; }

    public string Name { get; }

    public string? Description { get; }

    public Set<string> Subjects { get; }

    public string RefName { get; }

    public string RefHash { get; }

    public override bool Equals(object? obj) =>
      obj is SimEvidenceSource evidenceSource && Equals(evidenceSource);

    public bool Equals(SimEvidenceSource other) =>
      (ID, Name, Description, Subjects, RefName, RefHash) ==
      (other.ID, other.Name, other.Description, other.Subjects, other.RefName, other.RefHash);

    public override int GetHashCode() =>
      HashCode.Combine(ID, Name, Description, Subjects, RefName, RefHash);

    public static bool operator ==(SimEvidenceSource lhs, SimEvidenceSource rhs) =>
      lhs.Equals(rhs);

    public static bool operator !=(SimEvidenceSource lhs, SimEvidenceSource rhs) =>
      !(lhs == rhs);
  }
}
