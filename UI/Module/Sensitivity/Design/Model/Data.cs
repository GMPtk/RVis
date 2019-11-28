using LanguageExt;
using System.Data;
using System.Linq;
using static LanguageExt.Prelude;
using static Sensitivity.LowryPlotData;

namespace Sensitivity.Design
{
  internal static class Data
  {
    internal static DataView Samples
    {
      get
      {
        const int nColumns = 20;
        const int nRows = 100;
        var columnNames = Range(1, nColumns).Map(i => $"Column{i:000}");
        var dataTable = new DataTable();
        columnNames.Iter(cn => dataTable.Columns.Add(new DataColumn(cn, typeof(double))));
        for (var row = 0; row < nRows; ++row)
        {
          var dataRow = dataTable.NewRow();
          dataRow.ItemArray = Range(1, nColumns)
            .Map(c => 1.0 * c * row)
            .Cast<object>()
            .ToArray();
          dataTable.Rows.Add(dataRow);
        }
        return dataTable.DefaultView;
      }
    }

    internal static Arr<LowryParameterMeasure> LowryPlotDataFrom4DPieCharts
    {
      get
      {
        // https://4dpiecharts.com/2011/01/11/introducing-the-lowry-plot/

        var parameterNames = new[]
        {
          "BW", "CRE", "DS", "KM", "MPY", "Pba", "Pfaa",
          "Plia", "Prpda", "Pspda", "QCC", "QfaC", "QliC",
          "QPC", "QspdC", "Rurine", "Vfac", "VliC", "Vmax"
        };

        var mainEffects = new[]
        {
          1.03E-01, 9.91E-02, 9.18E-07, 3.42E-02, 9.27E-3, 2.82E-2, 2.58E-05,
          1.37E-05, 5.73E-4, 2.76E-3, 6.77E-3, 8.67E-05, 1.30E-02,
          1.19E-01, 4.75E-04, 5.25E-01, 2.07E-04, 1.73E-03, 1.08E-03
        };

        var interactions = new[]
        {
          1.49E-02, 1.43E-02, 1.25E-04, 6.84E-03, 3.25E-03, 7.67E-03, 8.34E-05,
          1.17E-04, 2.04E-04, 7.64E-04, 2.84E-03, 8.72E-05, 2.37E-03,
          2.61E-02, 6.68E-04, 4.57E-02, 1.32E-04, 6.96E-04, 6.55E-04
        };

        var parameterMeasures = Range(0, parameterNames.Length)
          .Map(i =>
            new LowryParameterMeasure(parameterNames[i])
            {
              Interaction = interactions[i],
              MainEffect = mainEffects[i]
            })
          .OrderByDescending(pd => pd.MainEffect)
          .ToArray();

        var upperBounds = ComputeUpperBounds(
          parameterMeasures.Select(pd => pd.MainEffect).ToArray(), 
          parameterMeasures.Select(pd => pd.Interaction).ToArray()
          );

        var lowerBounds = ComputeLowerBounds(
          parameterMeasures.Select(pd => pd.MainEffect).ToArray()
          );

        for (var i = 0; i < parameterMeasures.Length; ++i)
        {
          parameterMeasures[i].LowerBound = lowerBounds[i];
          parameterMeasures[i].UpperBound = upperBounds[i];
        }

        return parameterMeasures;
      }
    }
  }
}