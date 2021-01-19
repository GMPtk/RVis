using RDotNet;
using RVis.Base.Extensions;
using RVis.Data;
using RVis.Data.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RVis.ROps
{
  public static partial class ROpsApi
  {
    private static IEnumerable<NumDataColumn> MatrixToNumDataColumns(
      double[,] array,
      string[]? columnNames = default,
      int[]? excludedColumnIndexes = default
      )
    {
      var rlb = array.GetLowerBound(0);
      var rub = array.GetUpperBound(0);
      var nRows = rub - rlb + 1;
      var values = new double[nRows];
      var clb = array.GetLowerBound(1);
      var cub = array.GetUpperBound(1);
      var nColumns = cub - clb + 1;

      columnNames ??= Enumerable.Range(1, nColumns).Select(i => i.ToString()).ToArray();
      excludedColumnIndexes ??= Array.Empty<int>();

      var numDataColumns = new List<NumDataColumn>();

      for (var column = clb; column <= cub; ++column)
      {
        if (excludedColumnIndexes.Contains(column)) continue;

        for (var row = rlb; row <= rub; ++row)
        {
          values[row - rlb] = array[row, column];
        }

        numDataColumns.Add(values.ToDataColumn(columnNames[column]));
      }

      return numDataColumns;
    }

    private static void SexpToMatricesRec(
      SymbolicExpression sexp,
      String? elementName,
      IList<(string[]?, double[,])> matrices
      )
    {
      if (sexp.IsList())
      {
        var list = sexp.AsList();
        var length = list.Length;
        var names = list.Names;

        if (names == null)
        {
          names = Enumerable
            .Range(1, length)
            .Select(i => $"[[{i}]]")
            .ToArray();
        }

        for (var element = 0; element < length; ++element)
        {
          SexpToMatricesRec(list[element], names[element], matrices);
        }
      }
      else if (sexp.IsMatrix())
      {
        var matrix = sexp.AsNumericMatrix();
        var columnNames = matrix.ColumnNames;
        if (null == columnNames && 1 == matrix.ColumnCount && elementName.IsAString())
        {
          columnNames = new[] { elementName };
        }
        var array = matrix.ToArray();
        matrices.Add((columnNames, array));
      }
      else if (sexp.IsVector())
      {
        var vector = sexp.AsNumeric();

        var names = vector.Names;
        if (null == names && elementName.IsAString())
        {
          names = new[] { elementName };
        }

        double[,] array;
        var doubles = vector.ToArray();

        if (names?.Length == doubles.Length)
        {
          array = new double[1, doubles.Length];
          for (var i = 0; i < doubles.Length; ++i)
          {
            array[0, i] = doubles[i];
          }
        }
        else
        {
          array = new double[doubles.Length, 1];
          for (var i = 0; i < doubles.Length; ++i)
          {
            array[i, 0] = doubles[i];
          }
        }
        matrices.Add((names, array));
      }
      else
      {
        throw new Exception("Unhandled sexp type: " + sexp.Type.ToString());
      }
    }

    internal static (string[]? ColumnNames, double[,] Array)[] SexpToMatrices(SymbolicExpression sexp, string? name = null)
    {
      var matrices = new List<(string[]?, double[,])>();
      SexpToMatricesRec(sexp, name, matrices);
      return matrices.ToArray();
    }
  }
}
