﻿using ProtoBuf;
using System;
using System.Collections.Generic;

namespace RVis.Model
{
  [ProtoContract]
  public struct SimElement : IEquatable<SimElement>
  {
    public SimElement(string name, bool isIndependentVariable, string unit, string description)
    {
      _name = name;
      _isIndependentVariable = isIndependentVariable;
      _unit = unit;
      _description = description;
    }

    [ProtoIgnore]
    public string Name => _name;

    [ProtoIgnore]
    public bool IsIndependentVariable => _isIndependentVariable;

    [ProtoIgnore]
    public string Unit => _unit;

    [ProtoIgnore]
    public string Description => _description;

    [ProtoMember(1)]
    private readonly string _name;

    [ProtoMember(2)]
    private readonly bool _isIndependentVariable;

    [ProtoMember(3)]
    private readonly string _unit;

    [ProtoMember(4)]
    private readonly string _description;

    public bool Equals(SimElement rhs) =>
      _name == rhs._name &&
      _isIndependentVariable == rhs._isIndependentVariable &&
      _unit == rhs._unit &&
      _description == rhs._description;

    public override int GetHashCode()
    {
      var hashCode = 2131522645;
      hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(_name);
      hashCode = hashCode * -1521134295 + _isIndependentVariable.GetHashCode();
      hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(_unit);
      hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(_description);
      return hashCode;
    }

    public override bool Equals(object obj) =>
      obj is SimElement element ? Equals(element) : false;

    public static bool operator ==(SimElement element1, SimElement element2) =>
      element1.Equals(element2);

    public static bool operator !=(SimElement element1, SimElement element2) =>
      !(element1 == element2);
  }
}
