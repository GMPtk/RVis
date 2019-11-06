using ProtoBuf;
using System;
using System.Collections.Generic;

namespace RVis.Model
{
  [ProtoContract]
  public struct SimConfig : IEquatable<SimConfig>
  {
    public SimConfig(string title, string description, DateTime importedOn, SimCode code, SimInput input, SimOutput output)
    {
      _title = title;
      _description = description;
      _importedOn = importedOn;
      _simCode = code;
      _simInput = input;
      _simOutput = output;
    }

    [ProtoIgnore]
    public string Title => _title;

    [ProtoIgnore]
    public string Description => _description;

    [ProtoIgnore]
    public DateTime ImportedOn => _importedOn;

    [ProtoIgnore]
    public SimCode SimCode => _simCode;

    [ProtoIgnore]
    public SimInput SimInput => _simInput;

    [ProtoIgnore]
    public SimOutput SimOutput => _simOutput;

    public SimConfig With(SimInput SimInput = default) =>
      new SimConfig(
        Title,
        Description,
        ImportedOn,
        SimCode,
        SimInput == default ? this.SimInput : SimInput,
        SimOutput
        );

    public bool Equals(SimConfig rhs) =>
      _title == rhs._title &&
      _description == rhs._description &&
      _importedOn == rhs._importedOn &&
      _simCode == rhs._simCode &&
      _simInput == rhs._simInput &&
      _simOutput == rhs._simOutput;

    public override int GetHashCode()
    {
      var hashCode = -286846682;
      hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(_title);
      hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(_description);
      hashCode = hashCode * -1521134295 + _importedOn.GetHashCode();
      hashCode = hashCode * -1521134295 + EqualityComparer<SimCode>.Default.GetHashCode(_simCode);
      hashCode = hashCode * -1521134295 + EqualityComparer<SimInput>.Default.GetHashCode(_simInput);
      hashCode = hashCode * -1521134295 + EqualityComparer<SimOutput>.Default.GetHashCode(_simOutput);
      return hashCode;
    }

    public override bool Equals(object obj) =>
      obj is SimConfig config ? Equals(config) : false;

    [ProtoMember(1)]
    private readonly string _title;

    [ProtoMember(2)]
    private readonly string _description;

    [ProtoMember(3)]
    private readonly DateTime _importedOn;

    [ProtoMember(4)]
    private readonly SimCode _simCode;

    [ProtoMember(5)]
    private readonly SimInput _simInput;

    [ProtoMember(6)]
    private readonly SimOutput _simOutput;

    public static bool operator ==(SimConfig config1, SimConfig config2) =>
      config1.Equals(config2);

    public static bool operator !=(SimConfig config1, SimConfig config2) =>
      !(config1 == config2);
  }
}
