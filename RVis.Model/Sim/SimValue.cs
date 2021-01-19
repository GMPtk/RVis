using LanguageExt;
using ProtoBuf;
using System;

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

    public override int GetHashCode() =>
      HashCode.Combine(_name, _simElements);

    public override bool Equals(object? obj) =>
      obj is SimValue value && Equals(value);

    [ProtoBeforeSerialization]
#pragma warning disable IDE0051 // Remove unused private members
    private void OnSerializing()
#pragma warning restore IDE0051 // Remove unused private members
    {
      _simElementArray = SimElements.IsEmpty ? default : SimElements.ToArray();
    }

    [ProtoAfterDeserialization]
#pragma warning disable IDE0051 // Remove unused private members
    private void OnDeserialized()
#pragma warning restore IDE0051 // Remove unused private members
    {
      _simElements = _simElementArray == default ? default : _simElementArray.ToArr();
    }

    [ProtoMember(1)]
    private readonly string _name;

    [ProtoIgnore]
    private Arr<SimElement> _simElements;

    [ProtoMember(2)]
    private SimElement[]? _simElementArray;

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
