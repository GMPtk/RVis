using ProtoBuf;
using System;

namespace RVis.Model
{
  [ProtoContract]
  public struct SimCode : IEquatable<SimCode>
  {
    public const string R_ASSIGN_PARAMETERS = "assign_parameters";
    public const string R_RUN_MODEL = "run_model";

    public SimCode(string? file)
    {
      _file = file;
    }

    [ProtoIgnore]
    public string? File => _file;

    [ProtoMember(1)]
    private readonly string? _file;

    public bool Equals(SimCode rhs) =>
      _file == rhs._file;

    public override int GetHashCode() =>
      HashCode.Combine(_file);

    public override bool Equals(object? obj) =>
      obj is SimCode code && Equals(code);

    public static bool operator ==(SimCode code1, SimCode code2) =>
      code1.Equals(code2);

    public static bool operator !=(SimCode code1, SimCode code2) =>
      !(code1 == code2);
  }
}
