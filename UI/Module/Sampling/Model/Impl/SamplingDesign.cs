using CsvHelper;
using LanguageExt;
using Nett;
using RVis.Base.Extensions;
using RVis.Model;
using System;
using System.Data;
using System.IO;
using System.Linq;
using static RVis.Base.Check;
using static Sampling.Logger;
using static System.IO.Path;
using static System.Globalization.CultureInfo;

namespace Sampling
{
  internal sealed partial class SamplingDesign
  {
    internal static SamplingDesign CreateSamplingDesign(
      DateTime createdOn,
      Arr<(string Name, IDistribution Distribution)> parameterDistributions,
      LatinHypercubeDesign latinHypercubeDesign,
      RankCorrelationDesign rankCorrelationDesign,
      int? seed,
      DataTable samples,
      Arr<int> noDataIndices
      )
    {
      RequireFalse(parameterDistributions.IsEmpty);
      RequireTrue(parameterDistributions.ForAll(ps => ps.Distribution.IsConfigured));
      RequireNotNull(samples);
      RequireTrue(samples.Columns.Count > 0);
      RequireTrue(samples.Rows.Count > 0);
      RequireTrue(parameterDistributions.ForAll(ps => samples.Columns.Contains(ps.Name)));

      var designParameters = parameterDistributions.Map(ps => new DesignParameter(ps.Name, ps.Distribution));

      return new SamplingDesign(
        createdOn, 
        designParameters, 
        latinHypercubeDesign,
        rankCorrelationDesign, 
        seed, 
        samples, 
        noDataIndices
        );
    }

    private class _DesignParameterDTO
    {
      public string? Name { get; set; }
      public string? Distribution { get; set; }
    }

    private class _SamplingDesignDTO
    {
      public string? CreatedOn { get; set; }
      public _DesignParameterDTO[]? DesignParameters { get; set; }
      public _LatinHypercubeDesignDTO? LatinHypercubeDesign { get; set; }
      public _RankCorrelationDesignDTO? RankCorrelationDesign { get; set; }
      public int? Seed { get; set; }
      public int[]? NoDataIndices { get; set; }
    }

    private const string DESIGN_FILE_NAME = "design.toml";
    private const string DESIGN_SAMPLES_FILE_NAME = "samples.csv";
    private const string FILTER_CONFIG_FILE_NAME = "filter_config.toml";

    internal static void RemoveSamplingDesign(string pathToSamplingDesignsDirectory, DateTime createdOn)
    {
      var samplingDesignDirectory = createdOn.ToDirectoryName();
      var pathToSamplingDesignDirectory = Combine(pathToSamplingDesignsDirectory, samplingDesignDirectory);
      try
      {
        Directory.Delete(pathToSamplingDesignDirectory, true);
      }
      catch (Exception ex)
      {
        Log.Error(ex, $"Failed to remove sampling design from {pathToSamplingDesignDirectory}");
      }
    }

    internal static SamplingDesign LoadSamplingDesign(string pathToSamplingDesignsDirectory, DateTime createdOn)
    {
      var samplingDesignDirectory = createdOn.ToDirectoryName();
      var pathToSamplingDesignDirectory = Combine(pathToSamplingDesignsDirectory, samplingDesignDirectory);

      var pathToDesign = Combine(pathToSamplingDesignDirectory, DESIGN_FILE_NAME);
      Arr<DesignParameter> designParameters;
      LatinHypercubeDesign latinHypercubeDesign;
      RankCorrelationDesign rankCorrelationDesign;
      int? seed;
      Arr<int> noDataIndices;

      try
      {
        var dto = Toml.ReadFile<_SamplingDesignDTO>(pathToDesign);

        designParameters = dto.DesignParameters?
          .Select(dp => new DesignParameter(
            dp.Name.AssertNotNull(),
            Distribution.DeserializeDistribution(dp.Distribution).AssertSome()
            )
          )
          .OrderBy(dp => dp.Name.ToUpperInvariant())
          .ToArr() ?? default;
        latinHypercubeDesign = dto.LatinHypercubeDesign?.FromDTO() ?? default;
        rankCorrelationDesign = dto.RankCorrelationDesign?.FromDTO() ?? default;
        seed = dto.Seed;
        noDataIndices = dto.NoDataIndices.ToArr();
      }
      catch (Exception ex)
      {
        var message = $"Failed to load sampling design from {pathToDesign}";
        Log.Error(ex, message);
        throw new Exception(message);
      }

      var pathToDesignSamples = Combine(pathToSamplingDesignDirectory, DESIGN_SAMPLES_FILE_NAME);
      var samples = new DataTable();

      designParameters.Iter(
        dp => samples.Columns.Add(new DataColumn(dp.Name, typeof(double)))
        );

      try
      {
        using var streamReader = new StreamReader(pathToDesignSamples);
        using var csvReader = new CsvReader(streamReader, InvariantCulture);
        using var csvDataReader = new CsvDataReader(csvReader);
        
        samples.Load(csvDataReader);
      }
      catch (Exception ex)
      {
        var message = $"Failed to save design samples to {pathToDesignSamples}";
        Log.Error(ex, message);
        throw new Exception(message);
      }

      return new SamplingDesign(
        createdOn, 
        designParameters, 
        latinHypercubeDesign,
        rankCorrelationDesign, 
        seed, 
        samples, 
        noDataIndices
        );
    }

    private static void SaveDesign(
      DateTime createdOn,
      Arr<DesignParameter> designParameters,
      LatinHypercubeDesign latinHypercubeDesign,
      RankCorrelationDesign rankCorrelationDesign,
      int? seed,
      Arr<int> noDataIndices,
      string pathToSamplingDesignDirectory
      )
    {
      RequireDirectory(pathToSamplingDesignDirectory);
      RequireOrdered(designParameters, dp => dp.Name.ToUpperInvariant());

      var dto = new _SamplingDesignDTO
      {
        CreatedOn = createdOn.ToDirectoryName(),
        DesignParameters = designParameters
          .Map(dp => new _DesignParameterDTO { Name = dp.Name, Distribution = dp.Distribution.ToString() })
          .ToArray(),
        LatinHypercubeDesign = latinHypercubeDesign.ToDTO(),
        RankCorrelationDesign = rankCorrelationDesign.ToDTO(),
        Seed = seed,
        NoDataIndices = noDataIndices.ToArray()
      };

      var pathToDesign = Combine(pathToSamplingDesignDirectory, DESIGN_FILE_NAME);

      try
      {
        Toml.WriteFile(dto, pathToDesign);
      }
      catch (Exception ex)
      {
        var message = $"Failed to save sampling design to {pathToDesign}";
        Log.Error(ex, message);
        throw new Exception(message);
      }
    }

    internal static void SaveSamples(DataTable samples, string pathToSamplingDesignDirectory)
    {
      RequireDirectory(pathToSamplingDesignDirectory);
      RequireOrdered(
        samples.Columns.Cast<DataColumn>(), 
        dc => dc.ColumnName.ToUpperInvariant()
        );

      var pathToDesignSamples = Combine(pathToSamplingDesignDirectory, DESIGN_SAMPLES_FILE_NAME);

      try
      {
        using var streamWriter = new StreamWriter(pathToDesignSamples);
        using var csvWriter = new CsvWriter(streamWriter, InvariantCulture);
        
        foreach (DataColumn column in samples.Columns)
        {
          csvWriter.WriteField(column.ColumnName);
        }

        csvWriter.NextRecord();

        foreach (DataRow row in samples.Rows)
        {
          for (var i = 0; i < samples.Columns.Count; ++i)
          {
            csvWriter.WriteField(row[i]);
          }

          csvWriter.NextRecord();
        }
      }
      catch (Exception ex)
      {
        var message = $"Failed to save design samples to {pathToDesignSamples}";
        Log.Error(ex, message);
        throw new Exception(message);
      }
    }

    internal static void SaveSamplingDesign(SamplingDesign instance, string pathToSamplingDesignsDirectory)
    {
      RequireDirectory(pathToSamplingDesignsDirectory);

      var samplingDesignDirectory = instance.CreatedOn.ToDirectoryName();
      var pathToSamplingDesignDirectory = Combine(pathToSamplingDesignsDirectory, samplingDesignDirectory);

      RequireFalse(Directory.Exists(pathToSamplingDesignDirectory));
      Directory.CreateDirectory(pathToSamplingDesignDirectory);

      SaveDesign(
        instance.CreatedOn,
        instance.DesignParameters,
        instance.LatinHypercubeDesign,
        instance.RankCorrelationDesign,
        instance.Seed,
        instance.NoDataIndices,
        pathToSamplingDesignDirectory
        );

      SaveSamples(
        instance.Samples,
        pathToSamplingDesignDirectory
        );
    }

    internal static void UpdateSamplingDesign(SamplingDesign instance, string pathToSamplingDesignsDirectory)
    {
      RequireDirectory(pathToSamplingDesignsDirectory);

      var samplingDesignDirectory = instance.CreatedOn.ToDirectoryName();
      var pathToSamplingDesignDirectory = Combine(pathToSamplingDesignsDirectory, samplingDesignDirectory);

      RequireDirectory(pathToSamplingDesignDirectory);

      SaveDesign(
        instance.CreatedOn,
        instance.DesignParameters,
        instance.LatinHypercubeDesign,
        instance.RankCorrelationDesign,
        instance.Seed,
        instance.NoDataIndices,
        pathToSamplingDesignDirectory
        );

      SaveSamples(
        instance.Samples,
        pathToSamplingDesignDirectory
        );
    }

    internal static void SaveFilterConfig(SamplingDesign instance, FilterConfig filterConfig, string pathToSamplingDesignsDirectory)
    {
      RequireDirectory(pathToSamplingDesignsDirectory);

      var samplingDesignDirectory = instance.CreatedOn.ToDirectoryName();
      var pathToSamplingDesignDirectory = Combine(pathToSamplingDesignsDirectory, samplingDesignDirectory);

      RequireDirectory(pathToSamplingDesignDirectory);

      var pathToFilterConfig = Combine(pathToSamplingDesignDirectory, FILTER_CONFIG_FILE_NAME);

      var dto = filterConfig.ToDTO();

      try
      {
        Toml.WriteFile(dto, pathToFilterConfig);
      }
      catch (Exception ex)
      {
        var message = $"Failed to save filter config to {pathToFilterConfig}";
        Log.Error(ex, message);
        throw new Exception(message);
      }
    }

    internal static FilterConfig LoadFilterConfig(SamplingDesign instance, string pathToSamplingDesignsDirectory)
    {
      RequireDirectory(pathToSamplingDesignsDirectory);

      var samplingDesignDirectory = instance.CreatedOn.ToDirectoryName();
      var pathToSamplingDesignDirectory = Combine(pathToSamplingDesignsDirectory, samplingDesignDirectory);

      RequireDirectory(pathToSamplingDesignDirectory);

      var pathToFilterConfig = Combine(pathToSamplingDesignDirectory, FILTER_CONFIG_FILE_NAME);

      if (!File.Exists(pathToFilterConfig)) return FilterConfig.Default;
      
      try
      {
        var dto = Toml.ReadFile<_FilterConfigDTO>(pathToFilterConfig);
        return dto.FromDTO();
      }
      catch (Exception ex)
      {
        var message = $"Failed to load filter config from {pathToFilterConfig}";
        Log.Error(ex, message);
        throw new Exception(message);
      }
    }
  }
}
