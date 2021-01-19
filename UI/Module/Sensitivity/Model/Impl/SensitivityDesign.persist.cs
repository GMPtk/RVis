using CsvHelper;
using LanguageExt;
using Nett;
using ProtoBuf;
using RVis.Base.Extensions;
using RVis.Data;
using RVis.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using static RVis.Base.Check;
using static Sensitivity.Logger;
using static System.Double;
using static System.Globalization.CultureInfo;
using static System.IO.Path;
using DataTable = System.Data.DataTable;

namespace Sensitivity
{
  internal sealed partial class SensitivityDesign
  {
    private class _DesignParameterDTO
    {
      public string? Name { get; set; }
      public string? Distribution { get; set; }
    }

    private class _SensitivityDesignDTO
    {
      public string? CreatedOn { get; set; }
      public _DesignParameterDTO[]? DesignParameters { get; set; }
      public string? SensitivityMethod { get; set; }
      public string? MethodParameters { get; set; }
    }

    private class _RankingParameterDTO
    {
      public string? Name { get; set; }
      public double? Score { get; set; }
      public bool IsSelected { get; set; }
    }

    private class _RankingDTO
    {
      public double? XBegin { get; set; }
      public double? XEnd { get; set; }
      public string[]? Outputs { get; set; }
      public _RankingParameterDTO[]? Parameters { get; set; }
    }

    private const string DESIGN_FILE_NAME = "design.toml";
    private const string SERIALIZED_TRACE_FILE_NAME = "trace.bin";
    private const string RANKING_FILE_NAME = "ranking.toml";

    internal static void RemoveSensitivityDesign(
      string pathToSensitivityDesignsDirectory,
      DateTime createdOn
      )
    {
      var sensitivityDesignDirectory = createdOn.ToDirectoryName();

      var pathToSensitivityDesignDirectory = Combine(
        pathToSensitivityDesignsDirectory,
        sensitivityDesignDirectory
        );

      try
      {
        Directory.Delete(
          pathToSensitivityDesignDirectory,
          recursive: true
          );
      }
      catch (Exception ex)
      {
        Log.Error(
          ex,
          $"Failed to remove sensitivity design from {pathToSensitivityDesignDirectory}"
          );
      }
    }

    internal static SensitivityDesign LoadSensitivityDesign(
      string pathToSensitivityDesignsDirectory,
      DateTime createdOn
      )
    {
      var sensitivityDesignDirectory = createdOn.ToDirectoryName();
      var pathToSensitivityDesignDirectory = Combine(
        pathToSensitivityDesignsDirectory,
        sensitivityDesignDirectory
        );

      var pathToDesign = Combine(
        pathToSensitivityDesignDirectory,
        DESIGN_FILE_NAME
        );

      Arr<DesignParameter> designParameters;
      SensitivityMethod sensitivityMethod;
      string methodParameters;

      try
      {
        var dto = Toml.ReadFile<_SensitivityDesignDTO>(pathToDesign);

        RequireNotNull(dto.DesignParameters);
        designParameters = dto.DesignParameters
          .Select(dp => new DesignParameter(
            dp.Name.AssertNotNull(),
            Distribution.DeserializeDistribution(dp.Distribution).AssertSome()
            )
          )
          .ToArr();

        RequireNotNull(dto.SensitivityMethod);
        sensitivityMethod = (SensitivityMethod)Enum.Parse(
          typeof(SensitivityMethod),
          dto.SensitivityMethod
          );

        RequireNotNull(dto.MethodParameters);
        methodParameters = dto.MethodParameters;
      }
      catch (Exception ex)
      {
        var message = $"Failed to load sensitivity design from {pathToDesign}";
        Log.Error(ex, message);
        throw new Exception(message);
      }

      var samples = LoadSamples(designParameters, pathToSensitivityDesignDirectory);
      var serializedDesigns = LoadSerializedDesigns(pathToSensitivityDesignDirectory);

      return new SensitivityDesign(
        createdOn,
        serializedDesigns,
        designParameters,
        sensitivityMethod,
        methodParameters,
        samples
        );
    }

    internal static void SaveSensitivityDesign(
      SensitivityDesign instance,
      string pathToSensitivityDesignsDirectory
      )
    {
      RequireDirectory(pathToSensitivityDesignsDirectory);

      var sensitivityDesignDirectory = instance.CreatedOn.ToDirectoryName();
      var pathToSensitivityDesignDirectory = Combine(
        pathToSensitivityDesignsDirectory,
        sensitivityDesignDirectory
        );

      RequireFalse(Directory.Exists(pathToSensitivityDesignDirectory));
      Directory.CreateDirectory(pathToSensitivityDesignDirectory);

      SaveDesign(
        instance.CreatedOn,
        instance.DesignParameters,
        instance.SensitivityMethod,
        instance.MethodParameters,
        pathToSensitivityDesignDirectory
        );

      SaveSamples(
        instance.Samples,
        pathToSensitivityDesignDirectory
        );

      SaveSerializedDesigns(
        instance.SerializedDesigns,
        pathToSensitivityDesignDirectory
        );
    }

    internal static void SaveSensitivityDesignTrace(
      SensitivityDesign instance,
      NumDataTable trace,
      string pathToSensitivityDesignsDirectory
      )
    {
      RequireDirectory(pathToSensitivityDesignsDirectory);

      var sensitivityDesignDirectory = instance.CreatedOn.ToDirectoryName();
      var pathToSensitivityDesignDirectory = Combine(
        pathToSensitivityDesignsDirectory,
        sensitivityDesignDirectory
        );

      RequireDirectory(pathToSensitivityDesignDirectory);

      var pathToSerializedTrace = Combine(
        pathToSensitivityDesignDirectory,
        SERIALIZED_TRACE_FILE_NAME
        );

      using var memoryStream = new MemoryStream();
      Serializer.Serialize(memoryStream, trace);
      memoryStream.Position = 0;
      File.WriteAllBytes(pathToSerializedTrace, memoryStream.ToArray());
    }

    internal static NumDataTable? LoadSensitivityDesignTrace(
      SensitivityDesign instance,
      string pathToSensitivityDesignsDirectory
      )
    {
      RequireDirectory(pathToSensitivityDesignsDirectory);

      var sensitivityDesignDirectory = instance.CreatedOn.ToDirectoryName();
      var pathToSensitivityDesignDirectory = Combine(
        pathToSensitivityDesignsDirectory,
        sensitivityDesignDirectory
        );

      var pathToSerializedTrace = Combine(
        pathToSensitivityDesignDirectory,
        SERIALIZED_TRACE_FILE_NAME
        );

      if (File.Exists(pathToSerializedTrace))
      {
        var serializedTrace = File.ReadAllBytes(pathToSerializedTrace);
        using var memoryStream = new MemoryStream(serializedTrace);
        return Serializer.Deserialize<NumDataTable>(memoryStream);
      }

      return default;
    }

    internal static void SaveSensitivityDesignRanking(
      SensitivityDesign instance,
      Ranking ranking,
      string pathToSensitivityDesignsDirectory
    )
    {
      RequireDirectory(pathToSensitivityDesignsDirectory);

      var sensitivityDesignDirectory = instance.CreatedOn.ToDirectoryName();
      var pathToSensitivityDesignDirectory = Combine(
        pathToSensitivityDesignsDirectory,
        sensitivityDesignDirectory
        );

      RequireDirectory(pathToSensitivityDesignDirectory);

      var pathToRanking = Combine(
        pathToSensitivityDesignDirectory,
        RANKING_FILE_NAME
        );

      var dto = new _RankingDTO
      {
        XBegin = ranking.XBegin,
        XEnd = ranking.XEnd,
        Outputs = ranking.Outputs.ToArray(),
        Parameters = ranking.Parameters
        .Map(p => new _RankingParameterDTO
        {
          Name = p.Parameter,
          Score = IsNaN(p.Score) ? default(double?) : p.Score,
          IsSelected = p.IsSelected
        })
        .ToArray()
      };

      try
      {
        Toml.WriteFile(dto, pathToRanking);
      }
      catch (Exception ex)
      {
        var message = $"Failed to ranking to {pathToRanking}";
        Log.Error(ex, message);
        throw new Exception(message);
      }
    }

    internal static Ranking LoadSensitivityDesignRanking(
      SensitivityDesign instance,
      string pathToSensitivityDesignsDirectory
      )
    {
      RequireDirectory(pathToSensitivityDesignsDirectory);

      var sensitivityDesignDirectory = instance.CreatedOn.ToDirectoryName();
      var pathToSensitivityDesignDirectory = Combine(
        pathToSensitivityDesignsDirectory,
        sensitivityDesignDirectory
        );

      RequireDirectory(pathToSensitivityDesignDirectory);

      var pathToRanking = Combine(
        pathToSensitivityDesignDirectory,
        RANKING_FILE_NAME
        );

      if (File.Exists(pathToRanking))
      {
        try
        {
          var dto = Toml.ReadFile<_RankingDTO>(pathToRanking);
          var ranking = new Ranking(
            dto.XBegin,
            dto.XEnd,
            dto.Outputs ?? default,
            dto.Parameters?
              .Select(p => (p.Name.AssertNotNull(), p.Score ?? NaN, p.IsSelected))
              .ToArr() ?? default
            );
          return ranking;
        }
        catch (Exception ex)
        {
          var message = $"Failed to load ranking from {pathToRanking}";
          Log.Error(ex, message);
          throw new Exception(message);
        }
      }

      return default;
    }

    private static Arr<DataTable> LoadSamples(
      Arr<DesignParameter> designParameters,
      string pathToSensitivityDesignDirectory
    )
    {
      var samples = new List<DataTable>();
      var sampleNo = 1;

      do
      {
        var pathToDesignSamples = Combine(
          pathToSensitivityDesignDirectory,
          nameof(samples).ToLowerInvariant(),
          $"{sampleNo:0000}.csv"
          );

        if (!File.Exists(pathToDesignSamples)) break;

        var dataTable = new DataTable();

        designParameters
          .Filter(dp => dp.Distribution.DistributionType != DistributionType.Invariant)
          .Iter(dp => dataTable.Columns.Add(
            new DataColumn(dp.Name, typeof(double))
            ));

        try
        {
          using var streamReader = new StreamReader(pathToDesignSamples);
          using var csvReader = new CsvReader(streamReader, InvariantCulture);
          using var csvDataReader = new CsvDataReader(csvReader);
          dataTable.Load(csvDataReader);
        }
        catch (Exception ex)
        {
          var message = $"Failed to load design samples from {pathToDesignSamples}";
          Log.Error(ex, message);
          throw new Exception(message);
        }

        samples.Add(dataTable);

        ++sampleNo;
      }
      while (true);

      return samples.ToArr();
    }

    private static Arr<byte[]> LoadSerializedDesigns(string pathToSensitivityDesignDirectory)
    {
      var serializedDesigns = new List<byte[]>();
      var serializedDesignNo = 1;

      do
      {
        var pathToSerializedDesign = Combine(
          pathToSensitivityDesignDirectory,
          nameof(serializedDesigns).ToLowerInvariant(),
          $"{serializedDesignNo:0000}.bin"
          );

        if (!File.Exists(pathToSerializedDesign)) break;

        var serializedDesign = File.ReadAllBytes(pathToSerializedDesign);
        serializedDesigns.Add(serializedDesign);

        ++serializedDesignNo;
      }
      while (true);

      return serializedDesigns.ToArr();
    }

    private static void SaveDesign(
      DateTime createdOn,
      Arr<DesignParameter> designParameters,
      SensitivityMethod sensitivityMethod,
      string methodParameters,
      string pathToSensitivityDesignDirectory
      )
    {
      RequireDirectory(pathToSensitivityDesignDirectory);

      var dto = new _SensitivityDesignDTO
      {
        CreatedOn = createdOn.ToDirectoryName(),
        DesignParameters = designParameters
          .Map(dp => new _DesignParameterDTO
          {
            Name = dp.Name,
            Distribution = dp.Distribution.ToString()
          })
          .ToArray(),
        SensitivityMethod = sensitivityMethod.ToString(),
        MethodParameters = methodParameters
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

    private static void SaveSamples(
      Arr<DataTable> samples,
      string pathToSensitivityDesignDirectory
      )
    {
      RequireDirectory(pathToSensitivityDesignDirectory);

      var pathToDesignSamples = Combine(
        pathToSensitivityDesignDirectory,
        nameof(samples).ToLowerInvariant()
        );

      Directory.CreateDirectory(pathToDesignSamples);

      samples.Iter((i, dt) =>
      {
        var pathToCSV = Combine(
          pathToDesignSamples,
          $"{(i + 1):0000}.csv"
          );

        try
        {
          SaveDataTableToCSV(pathToCSV, dt);
        }
        catch (Exception ex)
        {
          var message = $"Failed to save design samples to {pathToCSV}";
          Log.Error(ex, message);
          throw new Exception(message);
        }
      });
    }

    private static void SaveSerializedDesigns(
      Arr<byte[]> serializedDesigns,
      string pathToSensitivityDesignDirectory
      )
    {
      RequireDirectory(pathToSensitivityDesignDirectory);

      var pathToSerializedDesigns = Combine(
        pathToSensitivityDesignDirectory,
        nameof(serializedDesigns).ToLowerInvariant()
        );

      Directory.CreateDirectory(pathToSerializedDesigns);

      serializedDesigns.Iter((i, ba) =>
      {
        var pathToSerializedDesign = Combine(
          pathToSerializedDesigns,
          $"{(i + 1):0000}.bin"
          );

        try
        {
          File.WriteAllBytes(pathToSerializedDesign, ba);
        }
        catch (Exception ex)
        {
          var message = $"Failed to save serialized design to {pathToSerializedDesign}";
          Log.Error(ex, message);
          throw new Exception(message);
        }
      });
    }
  }
}
