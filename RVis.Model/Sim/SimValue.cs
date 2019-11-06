using LanguageExt;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RVis.Model
{
  [ProtoContract]
  public struct SimValue : IEquatable<SimValue>
  {
    public SimValue(string name, Arr<SimElement> elements)
    {
      _name = name;
      _simElements = elements;
      _simElementArray = default;
    }

    [ProtoIgnore]
    public string Name => _name;

    [ProtoIgnore]
    public Arr<SimElement> SimElements => _simElements;

    public bool Equals(SimValue rhs) =>
      _name == rhs._name && _simElements == rhs._simElements;

    public override int GetHashCode()
    {
      var hashCode = 450469409;
      hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(_name);
      hashCode = hashCode * -1521134295 + EqualityComparer<Arr<SimElement>>.Default.GetHashCode(_simElements);
      return hashCode;
    }

    public override bool Equals(object obj) =>
      obj is SimValue value ? Equals(value) : false;

    [ProtoBeforeSerialization]
    private void OnSerializing()
    {
      _simElementArray = SimElements.IsEmpty ? default : SimElements.ToArray();
    }

    [ProtoAfterDeserialization]
    private void OnDeserialized()
    {
      _simElements = _simElementArray == default ? default : _simElementArray.ToArr();
    }

    [ProtoMember(1)]
    private readonly string _name;

    [ProtoIgnore]
    private Arr<SimElement> _simElements;

    [ProtoMember(2)]
    private SimElement[] _simElementArray;

    public static bool operator ==(SimValue value1, SimValue value2)
    {
      return value1.Equals(value2);
    }

    public static bool operator !=(SimValue value1, SimValue value2)
    {
      return !(value1 == value2);
    }
  }
}
