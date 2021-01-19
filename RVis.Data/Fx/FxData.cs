using CsvHelper;
using System.Data;
using System.IO;
using System.Linq;
using static LanguageExt.Prelude;
using static RVis.Base.Check;
using static System.Globalization.CultureInfo;
using FxDataTable = System.Data.DataTable;

namespace RVis.Data
{
  public static class FxData
  {
    public static FxDataTable LoadFromCSV<T>(string pathToCSV)
    {
      RequireFile(pathToCSV);

      using var streamReader = new StreamReader(pathToCSV);
      using var csvReader = new CsvReader(streamReader, InvariantCulture);

      var dataTable = new FxDataTable();

      csvReader.Read();
      csvReader.ReadHeader();

      foreach (var header in csvReader.Context.HeaderRecord)
      {
        dataTable.Columns.Add(new DataColumn(header, typeof(T)));
      }

      while (csvReader.Read())
      {
        var dataRow = dataTable.NewRow();
        dataRow.ItemArray = Range(0, dataTable.Columns.Count)
          .Map(i => csvReader.GetField<T>(i))
          .Cast<object>()
          .ToArray();
        dataTable.Rows.Add(dataRow);
      }

      return dataTable;
    }

    public static void SaveToCSV<T>(FxDataTable dataTable, string pathToCSV)
    {
      using var streamWriter = new StreamWriter(pathToCSV);
      using var csvWriter = new CsvWriter(streamWriter, InvariantCulture);

      foreach (DataColumn column in dataTable.Columns)
      {
        csvWriter.WriteField(column.ColumnName);
      }

      csvWriter.NextRecord();

      foreach (DataRow dataRow in dataTable.Rows)
      {
        for (var i = 0; i < dataTable.Columns.Count; ++i)
        {
          csvWriter.WriteField(dataRow.Field<T>(i));
        }

        csvWriter.NextRecord();
      }
    }
  }
}
