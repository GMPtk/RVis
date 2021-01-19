using Nett;
using RVis.Base.Extensions;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace RVis.Model
{
  public static class Sim
  {
    public static void WriteConfigToFile(TSimConfig config, string pathToFile) =>
      Toml.WriteFile(config, pathToFile);

    public static TSimConfig ReadConfigFromFile(string pathToFile) =>
      Toml.ReadFile<TSimConfig>(pathToFile);

    public static void SerializeConfig(SimConfig config, Stream stream) =>
#pragma warning disable SYSLIB0011 // Type or member is obsolete
      new BinaryFormatter().Serialize(stream, config);
#pragma warning restore SYSLIB0011 // Type or member is obsolete

    public static SimConfig DeserializeConfig(Stream stream) =>
#pragma warning disable SYSLIB0011 // Type or member is obsolete
      (SimConfig)new BinaryFormatter().Deserialize(stream);
#pragma warning restore SYSLIB0011 // Type or member is obsolete

    internal static SimInput FromToml(TSimInput input)
    {
      var parameters = input.Parameters?.IsNullOrEmpty() == true
        ? default
        : input.Parameters!
            .Select(p => new SimParameter(p.Name, p.Value, p.Unit, p.Description))
            .ToArr();
      return new SimInput(parameters, input.IsDefault);
    }

    internal static TSimConfig ToToml(SimConfig config)
    {
      return new TSimConfig
      {
        Title = config.Title,
        Description = config.Description,
        ImportedOn = config.ImportedOn,
        Code = new TSimCode
        {
          File = config.SimCode.File,
          Exec = config.SimCode.Exec,
          Formal = config.SimCode.Formal
        },
        Input = new TSimInput
        {
          Parameters = config.SimInput.SimParameters.IsEmpty
            ? default
            : config.SimInput.SimParameters.Map(p => new TSimParameter
              {
                Name = p.Name,
                Value = p.Value,
                Unit = p.Unit,
                Description = p.Description
              }).ToArray(),
          IsDefault = config.SimInput.IsDefault
        },
        Output = new TSimOutput
        {
          Values = config.SimOutput.SimValues.IsEmpty
            ? default
            : config.SimOutput.SimValues.Map(v => new TSimValue
              {
                Name = v.Name,
                Elements = v.SimElements.Map(e => new TSimElement
                {
                  Name = e.Name,
                  IsIndependentVariable = e.IsIndependentVariable,
                  Unit = e.Unit,
                  Description = e.Description
                }).ToArray()
              }).ToArray()
        }
      };
    }

    internal static SimConfig FromToml(TSimConfig config)
    {
      var code = new SimCode(config.Code?.File, config.Code?.Exec, config.Code?.Formal);

      var parameters = config.Input?.Parameters?.IsNullOrEmpty() == true
        ? default
        : config.Input!.Parameters!
            .Select(p => new SimParameter(p.Name, p.Value, p.Unit, p.Description))
            .ToArr();

      var input = new SimInput(parameters, isDefault: config.Input?.IsDefault == true);

      var values = config.Output?.Values?.IsNullOrEmpty() == true
        ? default
        : config.Output!.Values!
            .Select(v => new SimValue(v.Name, v.Elements?.IsNullOrEmpty() == true
              ? default
              : v.Elements!
                  .Select(e => new SimElement(e.Name, e.IsIndependentVariable, e.Unit, e.Description))
                  .ToArr()
              ))
            .ToArr();

      var output = new SimOutput(values);

      return new SimConfig(
        config.Title, 
        config.Description, 
        config.ImportedOn, 
        code, 
        input, 
        output
        );
    }
  }
}
