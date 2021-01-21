using LanguageExt;
using RVis.Base.Extensions;
using RVis.Model;
using System.Data;
using System.Linq;
using System.Reactive.Linq;
using static RVis.Base.Check;
using static System.Double;
using DataTable = System.Data.DataTable;

namespace Sensitivity
{
  internal sealed partial class DesignViewModel
  {
    private enum AcquiredType
    {
      Error,
      Suspect,
      No,
      Yes
    }

    private const string ACQUIRED_DATACOLUMN_NAME = "Acquired?";

    private void UpdateInputs(
      DataTable inputs,
      (SimInput Input, bool OutputRequested, Arr<double> Output)[] outputRequestJob
      )
    {
      Inputs = default;
      HasIssues = UpdateInputsImpl(inputs, outputRequestJob);
      var dataView = inputs.DefaultView;
      dataView.RowFilter = InputsRowFilter;
      Inputs = dataView;
    }

    private static bool UpdateInputsImpl(
      DataTable inputs,
      (SimInput Input, bool OutputRequested, Arr<double> Output)[] outputRequestJob
      )
    {
      RequireEqual(inputs.Rows.Count, outputRequestJob.Length);

      var acquiredColumn = inputs
        .Columns[ACQUIRED_DATACOLUMN_NAME]
        .AssertNotNull($"{ACQUIRED_DATACOLUMN_NAME} not found");
      var foundIssues = false;

      for (var i = 0; i < outputRequestJob.Length; ++i)
      {
        var (input, _, output) = outputRequestJob[i];
        var dataRow = inputs.Rows[i];

        var acquired = input == default
          ? AcquiredType.Error
          : output == default
            ? IsSuspect(dataRow)
              ? AcquiredType.Suspect
              : AcquiredType.No
            : IsSuspect(output)
              ? AcquiredType.Suspect
              : AcquiredType.Yes;

        if (acquired == AcquiredType.Suspect || acquired == AcquiredType.Error)
        {
          foundIssues = true;
        }

        dataRow[acquiredColumn] = acquired.ToString();
      }

      return foundIssues;
    }

    private static bool IsSuspect(DataRow dataRow) =>
      dataRow.ItemArray.OfType<double>().Any(d => IsInfinity(d) || IsNaN(d));

    private static bool IsSuspect(Arr<double> output) =>
      output.Exists(IsNaN);

    private static DataTable CreateInputs(Arr<DataTable> samples)
    {
      RequireFalse(samples.IsEmpty);

      var inputs = samples.Head().Clone();

      samples.Iter(dt =>
      {
        foreach (DataRow dataRow in dt.Rows)
        {
          inputs.Rows.Add(dataRow.ItemArray);
        }
      });

      var acquiredColumn = new DataColumn(ACQUIRED_DATACOLUMN_NAME, typeof(string));
      inputs.Columns.Add(acquiredColumn);

      return inputs;
    }
  }
}
