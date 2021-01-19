using LanguageExt;
using ProtoBuf;
using RVis.Base.Extensions;
using System;
using System.Collections.Generic;

namespace RVis.Model
{
  [ProtoContract]
  public struct SimOutput : IEquatable<SimOutput>
  {
    public SimOutput(Arr<SimValue> values)
    {
      _values = values;
      _valueArray = default;
      _independentVariable = default;
      _dependentVariables = default;
    }

    [ProtoIgnore]
    public Arr<SimValue> SimValues => _values;

    [ProtoIgnore]
    public SimElement IndependentVariable => _independentVariable.Name.IsntAString()
      ? (_independentVariable = GetIndependentVariable())
      : _independentVariable;

    [ProtoIgnore]
    public Arr<SimElement> DependentVariables => _dependentVariables.IsEmpty
      ? (_dependentVariables = GetDependentVariables())
      : _dependentVariables;

    private SimElement GetIndependentVariable() => SimValues
      .SelectMany(v => v.SimElements)
      .Single(e => e.IsIndependentVariable);

    private Arr<SimElement> GetDependentVariables() =>
      SimValues
        .Bind(v => v.SimElements)
        .Filter(e => !e.IsIndependentVariable)
        .ToArr();

    public bool Equals(SimOutput rhs) =>
      _values == rhs._values;

    public override int GetHashCode() =>
      -339605548 + EqualityComparer<Arr<SimValue>>.Default.GetHashCode(_values);

    public override bool Equals(object? obj) =>
      obj is SimOutput output && Equals(output);

    [ProtoBeforeSerialization]
#pragma warning disable IDE0051 // Remove unused private members
    private void OnSerializing()
#pragma warning restore IDE0051 // Remove unused private members
    {
      _valueArray = SimValues.IsEmpty ? default : SimValues.ToArray();
    }

    [ProtoAfterDeserialization]
#pragma warning disable IDE0051 // Remove unused private members
    private void OnDeserialized()
#pragma warning restore IDE0051 // Remove unused private members
    {
      _values = _valueArray == default ? default : _valueArray.ToArr();
    }

    [ProtoIgnore]
    private Arr<SimValue> _values;

    [ProtoMember(1)]
    private SimValue[]? _valueArray;

    [ProtoIgnore]
    private SimElement _independentVariable;

    [ProtoIgnore]
    private Arr<SimElement> _dependentVariables;

    public static bool operator ==(SimOutput output1, SimOutput output2) =>
      output1.Equals(output2);

    public static bool operator !=(SimOutput output1, SimOutput output2) =>
      !(output1 == output2);
  }
}
