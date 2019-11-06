using ProtoBuf;
using System;
using System.Collections.Generic;

namespace RVis.Model
{
  [ProtoContract]
  public struct SimCode : IEquatable<SimCode>
  {
    public SimCode(string file, string exec, string formal)
    {
      _file = file;
      _exec = exec;
      _formal = formal;
    }

    [ProtoIgnore]
    public string File => _file;

    [ProtoIgnore]
    public string Exec => _exec;

    [ProtoIgnore]
    public string Formal => _formal;

    [ProtoMember(1)]
    private readonly string _file;

    [ProtoMember(2)]
    private readonly string _exec;

    [ProtoMember(3)]
    private readonly string _formal;

    public bool Equals(SimCode rhs) =>
      _file == rhs._file && _exec == rhs._exec && _formal == rhs._formal;

    public override int GetHashCode()
    {
      var hashCode = -1933996224;
      hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(_file);
      hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(_exec);
      hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(_formal);
      return hashCode;
    }

    public override bool Equals(object obj) =>
      obj is SimCode code ? Equals(code) : false;

    public static bool operator ==(SimCode code1, SimCode code2) =>
      code1.Equals(code2);

    public static bool operator !=(SimCode code1, SimCode code2) =>
      !(code1 == code2);
  }
}
