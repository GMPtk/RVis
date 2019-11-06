using CsvHelper;
using LanguageExt;
using System;
using System.Data;
using System.IO;
using static LanguageExt.Prelude;
using static RVis.Base.Check;
using static Sensitivity.Logger;
using static System.IO.Path;

namespace Sensitivity
{
  internal sealed partial class SensitivityDesign
  {
    private const string FIRST_ORDER_FILE_NAME = "firstorder.csv";
    private const string TOTAL_ORDER_FILE_NAME = "totalorder.csv";
    private const string VARIANCE_FILE_NAME = "variance.csv";

    internal static void SaveOutputMeasures(
      SensitivityDesign instance,
      string outputName,
      DataTable firstOrder,
      DataTable totalOrder,
      DataTable variance,
      string pathToSensitivityDesignsDirectory
      )
    {
      RequireDirectory(pathToSensitivityDesignsDirectory);

      var sensitivityDesignDirectory = instance.CreatedOn.ToDirectoryName();
      var pathToSensitivityDesignDirectory = Combine(pathToSensitivityDesignsDirectory, sensitivityDesignDirectory);

      RequireDirectory(pathToSensitivityDesignDirectory);

      var pathToOutputMeasuresDirectory = Combine(pathToSensitivityDesignDirectory, "measures", outputName);
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

    internal static bool LoadOutputMeasures(
      SensitivityDesign instance,
      string outputName,
      string pathToSensitivityDesignsDirectory,
      out (DataTable FirstOrder, DataTable TotalOrder, DataTable Variance) measures
      )
    {
      measures = default;

      RequireDirectory(pathToSensitivityDesignsDirectory);

      var sensitivityDesignDirectory = instance.CreatedOn.ToDirectoryName();
      var pathToSensitivityDesignDirectory = Combine(pathToSensitivityDesignsDirectory, sensitivityDesignDirectory);

      RequireDirectory(pathToSensitivityDesignDirectory);

      var pathToOutputMeasuresDirectory = Combine(pathToSensitivityDesignDirectory, "measures", outputName);
      if (!Directory.Exists(pathToOutputMeasuresDirectory)) return false;

      try
      {
        var pathToFirstOrder = Combine(pathToOutputMeasuresDirectory, FIRST_ORDER_FILE_NAME);
        if (!File.Exists(pathToFirstOrder)) return false;
        var firstOrder = LoadDataTableFromCSV(pathToFirstOrder);

        var pathToTotalOrder = Combine(pathToOutputMeasuresDirectory, TOTAL_ORDER_FILE_NAME);
        if (!File.Exists(pathToTotalOrder)) return false;
        var totalOrder = LoadDataTableFromCSV(pathToTotalOrder);

        var pathToVariance = Combine(pathToOutputMeasuresDirectory, VARIANCE_FILE_NAME);
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
      using (var streamReader = new StreamReader(pathToCSV))
      {
        string[] header;

        using (var csvReader = new CsvReader(streamReader, leaveOpen: true))
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

        using (var csvReader = new CsvReader(streamReader))
        {
          using (var csvDataReader = new CsvDataReader(csvReader))
          {
            dataTable.Load(csvDataReader);
          }
        }

        return dataTable;
      }
    }

    private static void SaveDataTableToCSV(string pathToCSV, DataTable dataTable)
    {
      using (var streamWriter = new StreamWriter(pathToCSV))
      using (var csvWriter = new CsvWriter(streamWriter))
      {
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
}
