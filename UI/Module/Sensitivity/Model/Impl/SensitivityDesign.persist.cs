using CsvHelper;
using LanguageExt;
using Nett;
using ProtoBuf;
using RVis.Base.Extensions;
using RVis.Data;
using RVis.Model;
using System;
using System.Data;
using System.IO;
using System.Linq;
using static RVis.Base.Check;
using static Sensitivity.Logger;
using static System.IO.Path;
using DataTable = System.Data.DataTable;

namespace Sensitivity
{
  internal sealed partial class SensitivityDesign
  {
    private class _DesignParameterDTO
    {
      public string Name { get; set; }
      public string Distribution { get; set; }
    }

    private class _SensitivityDesignDTO
    {
      public string CreatedOn { get; set; }
      public _DesignParameterDTO[] DesignParameters { get; set; }
      public int SampleSize { get; set; }
    }

    private const string DESIGN_FILE_NAME = "design.toml";
    private const string DESIGN_SAMPLES_FILE_NAME = "design.csv";
    private const string SERIALIZED_DESIGN_FILE_NAME = "design.bin";
    private const string SERIALIZED_TRACE_FILE_NAME = "trace.bin";

    internal static void RemoveSensitivityDesign(string pathToSensitivityDesignsDirectory, DateTime createdOn)
    {
      var sensitivityDesignDirectory = createdOn.ToDirectoryName();
      var pathToSensitivityDesignDirectory = Combine(pathToSensitivityDesignsDirectory, sensitivityDesignDirectory);
      try
      {
        Directory.Delete(pathToSensitivityDesignDirectory, true);
      }
      catch (Exception ex)
      {
        Log.Error(ex, $"Failed to remove sensitivity design from {pathToSensitivityDesignDirectory}");
      }
    }

    internal static SensitivityDesign LoadSensitivityDesign(string pathToSensitivityDesignsDirectory, DateTime createdOn)
    {
      var sensitivityDesignDirectory = createdOn.ToDirectoryName();
      var pathToSensitivityDesignDirectory = Combine(pathToSensitivityDesignsDirectory, sensitivityDesignDirectory);

      var pathToDesign = Combine(pathToSensitivityDesignDirectory, DESIGN_FILE_NAME);
      Arr<DesignParameter> designParameters;
      int sampleSize;

      try
      {
        var dto = Toml.ReadFile<_SensitivityDesignDTO>(pathToDesign);
        designParameters = dto.DesignParameters
          .Select(dp => new DesignParameter(
            dp.Name,
            Distribution.DeserializeDistribution(dp.Distribution).AssertSome()
            )
          )
          .ToArr();
        sampleSize = dto.SampleSize;
      }
      catch (Exception ex)
      {
        var message = $"Failed to load sensitivity design from {pathToDesign}";
        Log.Error(ex, message);
        throw new Exception(message);
      }

      var pathToDesignSamples = Combine(pathToSensitivityDesignDirectory, DESIGN_SAMPLES_FILE_NAME);
      var samples = new DataTable();

      designParameters
        .Filter(dp => dp.Distribution.DistributionType != DistributionType.Invariant)
        .Iter(dp => samples.Columns.Add(new DataColumn(dp.Name, typeof(double))));

      try
      {
        using (var streamReader = new StreamReader(pathToDesignSamples))
        using (var csvReader = new CsvReader(streamReader))
        using (var csvDataReader = new CsvDataReader(csvReader))
        {
          samples.Load(csvDataReader);
        }
      }
      catch (Exception ex)
      {
        var message = $"Failed to save design samples to {pathToDesignSamples}";
        Log.Error(ex, message);
        throw new Exception(message);
      }

      var pathToSerializedDesign = Combine(pathToSensitivityDesignDirectory, SERIALIZED_DESIGN_FILE_NAME);
      var serializedDesign = File.ReadAllBytes(pathToSerializedDesign);

      return new SensitivityDesign(createdOn, serializedDesign, designParameters, sampleSize, samples);
    }

    internal static void SaveSamples(DataTable samples, string pathToSensitivityDesignDirectory)
    {
      RequireDirectory(pathToSensitivityDesignDirectory);

      var pathToDesignSamples = Combine(pathToSensitivityDesignDirectory, DESIGN_SAMPLES_FILE_NAME);

      try
      {
        SaveDataTableToCSV(pathToDesignSamples, samples);
      }
      catch (Exception ex)
      {
        var message = $"Failed to save design samples to {pathToDesignSamples}";
        Log.Error(ex, message);
        throw new Exception(message);
      }
    }

    internal static void SaveSerializedDesign(byte[] serializedDesign, string pathToSensitivityDesignDirectory)
    {
      RequireDirectory(pathToSensitivityDesignDirectory);

      var pathToSerializedDesign = Combine(pathToSensitivityDesignDirectory, SERIALIZED_DESIGN_FILE_NAME);
      File.WriteAllBytes(pathToSerializedDesign, serializedDesign);
    }

    internal static void SaveSensitivityDesign(SensitivityDesign instance, string pathToSensitivityDesignsDirectory)
    {
      RequireDirectory(pathToSensitivityDesignsDirectory);

      var sensitivityDesignDirectory = instance.CreatedOn.ToDirectoryName();
      var pathToSensitivityDesignDirectory = Combine(pathToSensitivityDesignsDirectory, sensitivityDesignDirectory);

      RequireFalse(Directory.Exists(pathToSensitivityDesignDirectory));
      Directory.CreateDirectory(pathToSensitivityDesignDirectory);

      SaveDesign(
        instance.CreatedOn,
        instance.DesignParameters,
        instance.SampleSize,
        pathToSensitivityDesignDirectory
        );

      SaveSamples(
        instance.Samples,
        pathToSensitivityDesignDirectory
        );

      SaveSerializedDesign(
        instance.SerializedDesign,
        pathToSensitivityDesignDirectory
        );
    }

    //internal static void UpdateSensitivityDesign(SensitivityDesign instance, string pathToSensitivityDesignsDirectory)
    //{
    //  RequireDirectory(pathToSensitivityDesignsDirectory);

    //  var sensitivityDesignDirectory = instance.CreatedOn.ToDirectoryName();
    //  var pathToSensitivityDesignDirectory = Combine(pathToSensitivityDesignsDirectory, sensitivityDesignDirectory);

    //  RequireDirectory(pathToSensitivityDesignDirectory);

    //  SaveDesign(
    //    instance.CreatedOn,
    //    instance.DesignParameters,
    //    instance.SampleSize,
    //    pathToSensitivityDesignDirectory
    //    );

    //  SaveSamples(
    //    instance.Samples,
    //    pathToSensitivityDesignDirectory
    //    );

    //  SaveSerializedDesign(
    //    instance.SerializedDesign,
    //    pathToSensitivityDesignDirectory
    //    );
    //}

    internal static void SaveSensitivityDesignTrace(SensitivityDesign instance, NumDataTable trace, string pathToSensitivityDesignsDirectory)
    {
      RequireDirectory(pathToSensitivityDesignsDirectory);

      var sensitivityDesignDirectory = instance.CreatedOn.ToDirectoryName();
      var pathToSensitivityDesignDirectory = Combine(pathToSensitivityDesignsDirectory, sensitivityDesignDirectory);

      RequireDirectory(pathToSensitivityDesignDirectory);

      var pathToSerializedTrace = Combine(pathToSensitivityDesignDirectory, SERIALIZED_TRACE_FILE_NAME);
      using (var memoryStream = new MemoryStream())
      {
        Serializer.Serialize(memoryStream, trace);
        memoryStream.Position = 0;
        File.WriteAllBytes(pathToSerializedTrace, memoryStream.ToArray());
      }
    }

    internal static NumDataTable LoadSensitivityDesignTrace(SensitivityDesign instance, string pathToSensitivityDesignsDirectory)
    {
      RequireDirectory(pathToSensitivityDesignsDirectory);

      var sensitivityDesignDirectory = instance.CreatedOn.ToDirectoryName();
      var pathToSensitivityDesignDirectory = Combine(pathToSensitivityDesignsDirectory, sensitivityDesignDirectory);

      var pathToSerializedTrace = Combine(pathToSensitivityDesignDirectory, SERIALIZED_TRACE_FILE_NAME);

      if (File.Exists(pathToSerializedTrace))
      {
        var serializedTrace = File.ReadAllBytes(pathToSerializedTrace);
        using (var memoryStream = new MemoryStream(serializedTrace))
        {
          return Serializer.Deserialize<NumDataTable>(memoryStream);
        }
      }

      return default;
    }

    private static void SaveDesign(
      DateTime createdOn,
      Arr<DesignParameter> designParameters,
      int sampleSize,
      string pathToSensitivityDesignDirectory
      )
    {
      RequireDirectory(pathToSensitivityDesignDirectory);

      var dto = new _SensitivityDesignDTO
      {
        CreatedOn = createdOn.ToDirectoryName(),
        DesignParameters = designParameters
          .Map(dp => new _DesignParameterDTO { Name = dp.Name, Distribution = dp.Distribution.ToString() })
          .ToArray(),
        SampleSize = sampleSize
      };

      var pathToDesign = Combine(pathToSensitivityDesignDirectory, DESIGN_FILE_NAME);

      try
      {
        Toml.WriteFile(dto, pathToDesign);
      }
      catch (Exception ex)
      {
        var message = $"Failed to save sensitivity design to {pathToDesign}";
        Log.Error(ex, message);
        throw new Exception(message);
      }
    }
  }
}
