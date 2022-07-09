using Nett;
using ProtoBuf;
using RVis.Base.Extensions;
using System;
using System.IO;
using static RVis.Base.Check;
using static RVis.Model.Constant;
using static RVis.Model.Sim;

namespace RVis.Model
{
  [ProtoContract]
  public struct Simulation : IEquatable<Simulation>
  {
    public static Simulation LoadFrom(string pathToSimulation)
    {
      var pathToPrivate = Path.Combine(pathToSimulation, PRIVATE_SUBDIRECTORY);
      RequireDirectory(pathToPrivate);

      var pathToConfig = Path.Combine(pathToPrivate, CONFIG_FILE_NAME);
      RequireFile(pathToConfig);
      var config = LoadConfigFrom(pathToConfig);

      var simulation = new Simulation(pathToSimulation, config);

      return simulation;
    }

    public static SimConfig LoadConfigFrom(string pathToConfig)
    {
      try
      {
        var fromToml = Toml.ReadFile<TSimConfig>(pathToConfig);
        return FromToml(fromToml);
      }
      catch (Exception ex)
      {
        throw new ArgumentException("Failed to load config file", nameof(pathToConfig), ex);
      }
    }

    public Simulation(string pathToSimulation, SimConfig simConfig)
    {
      _pathToSimulation = pathToSimulation;
      _simConfig = simConfig;
    }

    [ProtoIgnore]
    public string PathToSimulation => _pathToSimulation;

    [ProtoIgnore]
    public SimConfig SimConfig => _simConfig;

    [ProtoIgnore]
    public string? PathToCodeFile => SimConfig.SimCode.File.IsAString()
      ? Path.Combine(PathToSimulation, SimConfig.SimCode.File)
      : default;

    public bool Equals(Simulation rhs) =>
      _pathToSimulation == rhs._pathToSimulation && _simConfig == rhs._simConfig;

    public override bool Equals(object? obj) =>
      obj is Simulation simulation && Equals(simulation);

    public override int GetHashCode() =>
      HashCode.Combine(_pathToSimulation, _simConfig);

    [ProtoMember(1)]
    private readonly string _pathToSimulation;

    [ProtoMember(2)]
    private readonly SimConfig _simConfig;

    public static bool operator ==(Simulation left, Simulation right) =>
      left.Equals(right);

    public static bool operator !=(Simulation left, Simulation right) =>
      !(left == right);
  }
}
