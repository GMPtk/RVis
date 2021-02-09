using LanguageExt;
using Nett;
using ProtoBuf;
using RVis.Base.Extensions;
using RVis.Model.Extensions;
using System;
using System.IO;
using System.Text;
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

    public string PopulateTemplate(Arr<SimParameter> parameters)
    {
      RequireTrue(this.IsTmplType());
      RequireFile(PathToCodeFile);
      var template = File.ReadAllText(PathToCodeFile);

      var expectedParameterNames = SimConfig.SimInput.SimParameters.Map(p => p.Name);
      var fullyPopulated = expectedParameterNames.ForAll(
        n => parameters.ContainsParameter(n)
        );
      RequireTrue(fullyPopulated);

      var pathToPopulated = Path.GetTempFileName();
      File.Move(pathToPopulated, pathToPopulated + ".R");
      pathToPopulated += ".R";

      var populated = SubstitutePlaceholders(template, parameters);

      File.WriteAllText(pathToPopulated, populated);

      return pathToPopulated;
    }

    private static string SubstitutePlaceholders(string template, Arr<SimParameter> parameters)
    {
      var nSubstitutions = parameters.Count;

      var parts = template.Split(new string[] { "${" }, StringSplitOptions.None);

      RequireEqual(parts.Length, nSubstitutions + 1, "Incompatible parameter set and template: non-equal substitutions");

      if (0 == nSubstitutions) return template;

      var sb = new StringBuilder(parts[0]);

      for (var i = 1; i < parts.Length; ++i)
      {
        var part = parts[i];
        var posRBrace = part.IndexOf('}');

        RequireTrue(posRBrace.IsFound(), "No closing } in section: " + part);

        var name = part.Substring(0, posRBrace).Trim();
        var parameter = parameters
          .FindParameter(name)
          .AssertSome($"Template specifies unknown parameter: {name}");

        sb.Append(parameter.GetRValue());
        sb.Append(part[(posRBrace + 1)..]);
      }

      return sb.ToString();
    }

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
