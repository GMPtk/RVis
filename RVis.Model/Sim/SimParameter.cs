using ProtoBuf;
using System;
using System.Globalization;
using static System.Double;
using static System.Globalization.CultureInfo;

namespace RVis.Model
{
  [ProtoContract]
  public struct SimParameter : IEquatable<SimParameter>
  {
    public SimParameter(string name, string value, string? unit, string? description)
    {
      _name = name;
      _value = value;
      _unit = unit;
      _description = description;

      _scalar = value != default && TryParse(value, NumberStyles.Float, InvariantCulture, out double d)
        ? d
        : NaN;
    }

    [ProtoIgnore]
    public string Name => _name;

    [ProtoIgnore]
    public string Value => _value;

    [ProtoIgnore]
    public double Scalar => _scalar;

    [ProtoIgnore]
    public string? Unit => _unit;

    [ProtoIgnore]
    public string? Description => _description;

    public SimParameter With(string? Value = null) =>
      new SimParameter(this, Value ?? this.Value);

    public SimParameter With(double value) =>
      new SimParameter(this, value);

    public bool Equals(SimParameter rhs) =>
      _name == rhs._name &&
      _value == rhs._value &&
      ((IsNaN(_scalar) && IsNaN(rhs._scalar)) || _scalar == rhs._scalar) &&
      _unit == rhs._unit &&
      _description == rhs._description;

    public override bool Equals(object? obj) =>
      obj is SimParameter parameter && Equals(parameter);

    public override string ToString() =>
      $"{Name} = {Value:G4}{Unit}";

    public override int GetHashCode() =>
      HashCode.Combine(Name, Value, Scalar, Unit, Description);

    private SimParameter(SimParameter toCopy, string value)
      : this(toCopy.Name, value, toCopy.Unit, toCopy.Description)
    {
    }

    private SimParameter(SimParameter toCopy, double value)
      : this(toCopy.Name, default!, toCopy.Unit, toCopy.Description)
    {
      _value = value.ToString(InvariantCulture);
      _scalar = value;
    }

    public static bool operator ==(SimParameter left, SimParameter right) =>
      left.Equals(right);

    public static bool operator !=(SimParameter left, SimParameter right) =>
      !(left == right);

    [ProtoMember(1)]
    private readonly string _name;

    [ProtoMember(2)]
    private readonly string _value;

    [ProtoMember(3)]
    private readonly double _scalar;

    [ProtoMember(4)]
    private readonly string? _unit;

    [ProtoMember(5)]
    private readonly string? _description;
  }
}
