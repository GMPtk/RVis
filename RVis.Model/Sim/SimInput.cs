using LanguageExt;
using ProtoBuf;
using RVis.Base.Extensions;
using RVis.Model.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RVis.Model
{
  [ProtoContract]
  public struct SimInput : IEquatable<SimInput>
  {
    public SimInput(Arr<SimParameter> parameters, bool isDefault)
    {
      _parameters = parameters;
      _parameterArray = default;
      _isDefault = isDefault;
      _hash = default;
    }

    [ProtoIgnore]
    public Arr<SimParameter> SimParameters => _parameters;

    [ProtoIgnore]
    public bool IsDefault => _isDefault;

    [ProtoIgnore]
    public string Hash => _hash.IsntAString() ? (_hash = GenerateHash()) : _hash;

    public SimInput With(Arr<SimParameter> SimParameters = default)
    {
      if (SimParameters == default) SimParameters = this.SimParameters;

      var withEdited = this.SimParameters.Map(
        p => SimParameters.FindParameter(p).Match(e => e, () => p)
      );

      return new SimInput(withEdited.ToArr(), isDefault: false);
    }

    public bool Equals(SimInput rhs) =>
      _parameters == rhs._parameters && _isDefault == rhs._isDefault;

    public override bool Equals(object obj) =>
      obj is SimInput rhs ? Equals(rhs) : false;

    public override int GetHashCode()
    {
      var hashCode = -537398035;
      hashCode = hashCode * -1521134295 + EqualityComparer<Arr<SimParameter>>.Default.GetHashCode(_parameters);
      hashCode = hashCode * -1521134295 + _isDefault.GetHashCode();
      return hashCode;
    }

    public static bool operator ==(SimInput lhs, SimInput rhs) =>
      lhs.Equals(rhs);

    public static bool operator !=(SimInput lhs, SimInput rhs) =>
      !(lhs == rhs);

    private string GenerateHash()
    {
      var sb = new StringBuilder();
      SimParameters.Iter(p => sb.Append(p.Name + p.Value));
      return sb.ToString().ToMD5Hash();
    }

    [ProtoBeforeSerialization]
    private void OnSerializing()
    {
      _parameterArray = SimParameters.IsEmpty ? default : SimParameters.ToArray();
    }

    [ProtoAfterDeserialization]
    private void OnDeserialized()
    {
      _parameters = _parameterArray == default ? default : _parameterArray.ToArr();
    }

    [ProtoIgnore]
    private Arr<SimParameter> _parameters;

    [ProtoMember(1)]
    private SimParameter[] _parameterArray;

    [ProtoMember(2)]
    private bool _isDefault;

    [ProtoIgnore]
    private string _hash;
  }
}
