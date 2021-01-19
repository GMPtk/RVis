using CsvHelper;
using System;
using System.Data;
using System.IO;
using static RVis.Base.Check;
using static Sensitivity.Logger;
using static System.IO.Path;
using static System.Globalization.CultureInfo;

namespace Sensitivity
{
  internal sealed partial class SensitivityDesign
  {
    private const string MEASURES_DIRECTORY_NAME = "measures";

    private const string MU_FILE_NAME = "mu.csv";
    private const string MU_STAR_FILE_NAME = "mustar.csv";
    private const string SIGMA_FILE_NAME = "sigma.csv";

    private const string FIRST_ORDER_FILE_NAME = "firstorder.csv";
    private const string TOTAL_ORDER_FILE_NAME = "totalorder.csv";
    private const string VARIANCE_FILE_NAME = "variance.csv";

    internal static void SaveMorrisOutputMeasures(
      SensitivityDesign instance,
      string outputName,
      DataTable mu,
      DataTable muStar,
      DataTable sigma,
      string pathToSensitivityDesignsDirectory
      )
    {
      RequireTrue(instance.SensitivityMethod == SensitivityMethod.Morris);
      RequireDirectory(pathToSensitivityDesignsDirectory);

      var sensitivityDesignDirectory = instance.CreatedOn.ToDirectoryName();
      var pathToSensitivityDesignDirectory = Combine(
        pathToSensitivityDesignsDirectory,
        sensitivityDesignDirectory
        );

      SaveMorrisOutputMeasures(
        outputName,
        mu,
        muStar, 
        sigma, 
        pathToSensitivityDesignDirectory
        );
    }

    internal static void SaveMorrisOutputMeasures(
      string outputName,
      DataTable mu,
      DataTable muStar,
      DataTable sigma,
      string pathToSensitivityDesignDirectory
      )
    {
      RequireDirectory(pathToSensitivityDesignDirectory);

      var pathToOutputMeasuresDirectory = Combine(
        pathToSensitivityDesignDirectory,
        MEASURES_DIRECTORY_NAME,
        outputName
        );

      Directory.CreateDirectory(pathToOutputMeasuresDirectory);

      try
      {
        var pathToFirstOrder = Combine(pathToOutputMeasuresDirectory, MU_FILE_NAME);
        SaveDataTableToCSV(pathToFirstOrder, mu);

        var pathToTotalOrder = Combine(pathToOutputMeasuresDirectory, MU_STAR_FILE_NAME);
        SaveDataTableToCSV(pathToTotalOrder, muStar);

        var pathToVariance = Combine(pathToOutputMeasuresDirectory, SIGMA_FILE_NAME);
        SaveDataTableToCSV(pathToVariance, sigma);
      }
      catch (Exception ex)
      {
        var message = $"Failed to save {outputName} measures to {pathToOutputMeasuresDirectory}";
        Log.Error(ex, message);
        throw new Exception(message);
      }
    }

    internal static void SaveFast99OutputMeasures(
      SensitivityDesign instance,
      string outputName,
      DataTable firstOrder,
      DataTable totalOrder,
      DataTable variance,
      string pathToSensitivityDesignsDirectory
      )
    {
      RequireTrue(instance.SensitivityMethod == SensitivityMethod.Fast99);
      RequireDirectory(pathToSensitivityDesignsDirectory);

      var sensitivityDesignDirectory = instance.CreatedOn.ToDirectoryName();
      var pathToSensitivityDesignDirectory = Combine(
        pathToSensitivityDesignsDirectory, 
        sensitivityDesignDirectory
        );

      SaveFast99OutputMeasures(
        outputName, 
        firstOrder, 
        totalOrder, 
        variance, 
        pathToSensitivityDesignDirectory
        );
    }

    internal static void SaveFast99OutputMeasures(
      string outputName,
      DataTable firstOrder,
      DataTable totalOrder,
      DataTable variance,
      string pathToSensitivityDesignDirectory
      )
    {
      RequireDirectory(pathToSensitivityDesignDirectory);

      var pathToOutputMeasuresDirectory = Combine(
        pathToSensitivityDesignDirectory,
        MEASURES_DIRECTORY_NAME,
        outputName
        );

      Directory.CreateDirectory(pathToOutputMeasuresDirectory);

      try
      {
        var pathToFirstOrder = Combine(pathToOutputMeasuresDirectory, FIRST_ORDER_FILE_NAME);
        SaveDataTableToCSV(pathToFirstOrder, firstOrder);

        var pathToTotalOrder = Combine(pathToOutputMeasuresDirectory, TOTAL_ORDER_FILE_NAME);
        SaveDataTableToCSV(pathToTotalOrder, totalOrder);

        var pathToVariance = Combine(pathToOutputMeasuresDirectory, VARIANCE_FILE_NAME);
        SaveDataTableToCSV(pathToVariance, variance);
      }
      catch (Exception ex)
      {
        var message = $"Failed to save {outputName} measures to {pathToOutputMeasuresDirectory}";
        Log.Error(ex, message);
        throw new Exception(message);
      }
    }

    internal static bool LoadMorrisOutputMeasures(
      SensitivityDesign instance,
      string outputName,
      string pathToSensitivityDesignsDirectory,
      out (DataTable Mu, DataTable MuStar, DataTable Sigma) measures
      )
    {
      RequireTrue(instance.SensitivityMethod == SensitivityMethod.Morris);

      measures = default;

      RequireDirectory(pathToSensitivityDesignsDirectory);

      var sensitivityDesignDirectory = instance.CreatedOn.ToDirectoryName();

      var pathToSensitivityDesignDirectory = Combine(
        pathToSensitivityDesignsDirectory,
        sensitivityDesignDirectory
        );

      RequireDirectory(pathToSensitivityDesignDirectory);

      var pathToOutputMeasuresDirectory = Combine(
        pathToSensitivityDesignDirectory,
        MEASURES_DIRECTORY_NAME,
        outputName
        );

      if (!Directory.Exists(pathToOutputMeasuresDirectory)) return false;

      try
      {
        var pathToMu = Combine(
          pathToOutputMeasuresDirectory,
          MU_FILE_NAME
          );
        if (!File.Exists(pathToMu)) return false;
        var mu = LoadDataTableFromCSV(pathToMu);

        var pathToMuStar = Combine(
          pathToOutputMeasuresDirectory,
          MU_STAR_FILE_NAME
          );
        if (!File.Exists(pathToMuStar)) return false;
        var muStar = LoadDataTableFromCSV(pathToMuStar);

        var pathToSigma = Combine(
          pathToOutputMeasuresDirectory,
          SIGMA_FILE_NAME
          );
        if (!File.Exists(pathToSigma)) return false;
        var sigma = LoadDataTableFromCSV(pathToSigma);

        measures = (mu, muStar, sigma);
        return true;
      }
      catch (Exception ex)
      {
        var message = $"Failed to load {outputName} measures from {pathToOutputMeasuresDirectory}";
        Log.Error(ex, message);
        throw new Exception(message);
      }
    }

    internal static bool LoadFast99OutputMeasures(
      SensitivityDesign instance,
      string outputName,
      string pathToSensitivityDesignsDirectory,
      out (DataTable FirstOrder, DataTable TotalOrder, DataTable Variance) measures
      )
    {
      RequireTrue(instance.SensitivityMethod == SensitivityMethod.Fast99);

      measures = default;

      RequireDirectory(pathToSensitivityDesignsDirectory);

      var sensitivityDesignDirectory = instance.CreatedOn.ToDirectoryName();

      var pathToSensitivityDesignDirectory = Combine(
        pathToSensitivityDesignsDirectory, 
        sensitivityDesignDirectory
        );

      RequireDirectory(pathToSensitivityDesignDirectory);

      var pathToOutputMeasuresDirectory = Combine(
        pathToSensitivityDesignDirectory,
        MEASURES_DIRECTORY_NAME, 
        outputName
        );

      if (!Directory.Exists(pathToOutputMeasuresDirectory)) return false;

      try
      {
        var pathToFirstOrder = Combine(
          pathToOutputMeasuresDirectory, 
          FIRST_ORDER_FILE_NAME
          );
        if (!File.Exists(pathToFirstOrder)) return false;
        var firstOrder = LoadDataTableFromCSV(pathToFirstOrder);

        var pathToTotalOrder = Combine(
          pathToOutputMeasuresDirectory, 
          TOTAL_ORDER_FILE_NAME
          );
        if (!File.Exists(pathToTotalOrder)) return false;
        var totalOrder = LoadDataTableFromCSV(pathToTotalOrder);

        var pathToVariance = Combine(
          pathToOutputMeasuresDirectory, 
          VARIANCE_FILE_NAME
          );
        if (!File.Exists(pathToVariance)) return false;
        var variance = LoadDataTableFromCSV(pathToVariance);

        measures = (firstOrder, totalOrder, variance);
        return true;
      }
      catch (Exception ex)
      {
        var message = $"Failed to load {outputName} measures from {pathToOutputMeasuresDirectory}";
        Log.Error(ex, message);
        throw new Exception(message);
      }
    }

    private static DataTable LoadDataTableFromCSV(string pathToCSV)
    {
      using var streamReader = new StreamReader(pathToCSV);
      
      string[] header;

      using (var csvReader = new CsvReader(streamReader, InvariantCulture, leaveOpen: true))
      {
        csvReader.Read();
        csvReader.ReadHeader();
        header = csvReader.Context.HeaderRecord;
      }

      var dataTable = new DataTable();
      foreach (var name in header)
      {
        dataTable.Columns.Add(new DataColumn(name, typeof(double)));
      }

      streamReader.BaseStream.Position = 0;

      using (var csvReader = new CsvReader(streamReader, InvariantCulture))
      {
        using var csvDataReader = new CsvDataReader(csvReader);
        dataTable.Load(csvDataReader);
      }

      return dataTable;
    }

    private static void SaveDataTableToCSV(string pathToCSV, DataTable dataTable)
    {
      using var streamWriter = new StreamWriter(pathToCSV);
      using var csvWriter = new CsvWriter(streamWriter, InvariantCulture);
      
      foreach (DataColumn column in dataTable.Columns)
      {
        csvWriter.WriteField(column.ColumnName);
      }

      csvWriter.NextRecord();

      foreach (DataRow row in dataTable.Rows)
      {
        for (var i = 0; i < dataTable.Columns.Count; ++i)
        {
          csvWriter.WriteField(row[i]);
        }

        csvWriter.NextRecord();
      }
    }
  }
}
