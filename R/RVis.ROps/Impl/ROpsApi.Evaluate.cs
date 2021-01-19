using RDotNet;
using RDotNet.Internals;
using System;
using System.Collections.Generic;
using System.Linq;
using static RVis.Base.Check;
using static System.Linq.Enumerable;

namespace RVis.ROps
{
  public static partial class ROpsApi
  {
    private static string[] CreateNames(int nColumns) =>
      Range(1, nColumns)
        .Select(i => $"{i:0000}")
        .ToArray();

    private static Dictionary<string, U[]?> ArrayToDictionary<T, U>(T[,] array, string[] columnNames)
    {
      var nRows = array.GetLength(0);
      var nColumns = array.GetLength(1);
      columnNames ??= CreateNames(nColumns);
      var dictionary = new Dictionary<string, U[]?>(nColumns);
      for (var column = 0; column < nColumns; ++column)
      {
        var data = new U[nRows];
        for (var row = 0; row < nRows; ++row)
        {
          var u = Convert.ChangeType(array[row, column], typeof(U));
          RequireNotNull(u, $"Failed to convert {array[row, column]} to {typeof(U)}");
          data[row] = (U)u;
        }
        dictionary.Add(columnNames[column], data);
      }
      return dictionary;
    }

    private static Dictionary<string, object[]?> MatrixToDictionary(SymbolicExpression sexp)
    {
      RequireTrue(sexp.IsMatrix());

      switch (sexp.Type)
      {
        case SymbolicExpressionType.CharacterVector:
          var characterMatrix = sexp.AsCharacterMatrix();
          return ArrayToDictionary<string, object>(characterMatrix.ToArray(), characterMatrix.ColumnNames);

        case SymbolicExpressionType.IntegerVector:
          var integerMatrix = sexp.AsIntegerMatrix();
          return ArrayToDictionary<int, object>(integerMatrix.ToArray(), integerMatrix.ColumnNames);

        case SymbolicExpressionType.LogicalVector:
          var logicalMatrix = sexp.AsLogicalMatrix();
          return ArrayToDictionary<bool, object>(logicalMatrix.ToArray(), logicalMatrix.ColumnNames);

        case SymbolicExpressionType.NumericVector:
          var numericMatrix = sexp.AsNumericMatrix();
          return ArrayToDictionary<double, object>(numericMatrix.ToArray(), numericMatrix.ColumnNames);

        default: throw new InvalidOperationException($"Unsupported matrix type: {sexp.Type}");
      }
    }

    private static object[]? VectorToArray(SymbolicExpression sexp)
    {
      RequireTrue(sexp.IsVector());

      return sexp.Type switch
      {
        SymbolicExpressionType.CharacterVector => sexp.AsCharacter().ToArray(),
        SymbolicExpressionType.IntegerVector => sexp.AsInteger().ToArray().Cast<object>().ToArray(),
        SymbolicExpressionType.LogicalVector => sexp.AsLogical().ToArray().Cast<object>().ToArray(),
        SymbolicExpressionType.NumericVector => sexp.AsNumeric().ToArray().Cast<object>().ToArray(),
        _ => default,
      };
    }

    private static Dictionary<string, T[]?> ListToDictionary<T>(GenericVector list, Func<SymbolicExpression, T[]?> elementToArray)
    {
      var nElements = list.Length;

      var names = list.Names;
      names ??= CreateNames(nElements);

      var dictionary = new Dictionary<string, T[]?>();

      for (var index = 0; index < nElements; ++index)
      {
        var name = names[index];
        var element = list[index];
        if (element.Type == SymbolicExpressionType.Null)
        {
          dictionary.Add(name, default);
          continue;
        }
        RequireTrue(element.IsVector(), $"List element {name} is not a vector (type is {element.Type})");
        dictionary.Add(name, elementToArray(element));
      }

      return dictionary;
    }

    private static Dictionary<string, object[]?> SexpToDictionary(SymbolicExpression sexp)
    {
      if (sexp.IsMatrix())
      {
        return MatrixToDictionary(sexp);
      }
      else if (sexp.IsList())
      {
        return ListToDictionary(sexp.AsList(), VectorToArray);
      }
      else if (sexp.IsVector())
      {
        var array = VectorToArray(sexp);
        if (array != default)
        {
          return new Dictionary<string, object[]?> { ["1"] = array };
        }
      }

      throw new ArgumentException($"SEXP of type {sexp.Type} cannot be converted to object lookup");
    }

    private static Dictionary<string, string[]?> SexpToStringDictionary(SymbolicExpression sexp)
    {
      if (sexp.IsMatrix())
      {
        var characterMatrix = sexp.AsCharacterMatrix();
        return ArrayToDictionary<string, string>(characterMatrix.ToArray(), characterMatrix.ColumnNames);
      }
      else if (sexp.IsList())
      {
        return ListToDictionary(sexp.AsList(), e => e.AsCharacter().ToArray());
      }
      else if (sexp.IsVector())
      {
        var array = sexp.AsCharacter().ToArray();
        if (array != default)
        {
          return new Dictionary<string, string[]?> { ["1"] = array };
        }
      }

      throw new ArgumentException($"SEXP of type {sexp.Type} cannot be converted to character lookup");
    }

    private static Dictionary<string, double[]?> SexpToDoubleDictionary(SymbolicExpression sexp)
    {
      if (sexp.IsMatrix())
      {
        var numericMatrix = sexp.AsNumericMatrix();
        return ArrayToDictionary<double, double>(numericMatrix.ToArray(), numericMatrix.ColumnNames);
      }
      else if (sexp.IsList())
      {
        return ListToDictionary(sexp.AsList(), e => e.AsNumeric().ToArray());
      }
      else if (sexp.IsVector())
      {
        var array = sexp.AsNumeric().ToArray();
        if (array != default)
        {
          return new Dictionary<string, double[]?> { ["1"] = array };
        }
      }

      throw new ArgumentException($"SEXP of type {sexp.Type} cannot be converted to numeric lookup");
    }
  }
}
