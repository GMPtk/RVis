using LanguageExt;
using MathNet.Numerics.LinearAlgebra;
using RVis.Base.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using static RVis.Base.Check;
using static System.Globalization.CultureInfo;
using static System.Linq.Enumerable;
using static System.String;
using static System.Convert;

namespace Sampling
{
  internal static class ModelExt
  {
    internal static bool IsPositiveDefinite(
      this Arr<(string parameterName, Arr<double> correlations)> correlations
      ) => correlations.CorrelationsToMatrix().IsPositiveDefinite();

    internal static bool IsPositiveDefinite(
      this double[,] correlations
      ) => correlations.GetRealEigenValues().ForAll(r => r > 0d);

    internal static Arr<double> GetRealEigenValues(this double[,] correlations)
    {
      var M = Matrix<double>.Build;
      var matrix = M.DenseOfArray(correlations);
      var evd = matrix.Evd(Symmetricity.Symmetric);
      var eigenValues = evd.EigenValues;
      RequireFalse(eigenValues.Any(c => c.Imaginary != 0d));
      return eigenValues.Select(ev => ev.Real).ToArr();
    }

    internal static T[,] CorrelationsToMatrix<T>(
      this Arr<(string _, Arr<T> Correlations)> correlations
      )
    {
      var n = correlations.Count;
      var matrix = new T[n, n];
      var diagonalT = (T)ChangeType(1, typeof(T));
      Range(0, n).Iter(i =>
      {
        matrix[i, i] = diagonalT;
        var cs = correlations[i].Correlations;
        Range(0, cs.Count).Iter(j =>
        {
          matrix[i, i + j + 1] = cs[j];
          matrix[i + j + 1, i] = cs[j];
        });
      });
      return matrix;
    }

    internal static bool IsValidCorrelations(
      this Arr<(string Parameter, Arr<double> Correlations)> correlations
      )
    {
      var parameters = correlations.Map(c => c.Parameter);

      var isInAscendingOrder = parameters.SequenceEqual(parameters.OrderBy(n => n.ToUpperInvariant()));
      if (!isInAscendingOrder) return false;

      var hasExpectedArrayLengths = correlations
        .Reverse()
        .Map((i, c) => c.Correlations.Count - i)
        .ForAll(i => i == 0);
      if (!hasExpectedArrayLengths) return false;

      return true;
    }

    internal static Arr<(string Parameter, Arr<double> Correlations)> UpdateCorrelations(
      this Arr<(string Parameter, Arr<double> Correlations)> correlations,
      Arr<(string Parameter, Arr<double> Correlations)> update
      )
    {
      var parametersToAdd = update
        .Filter(u => !correlations.Exists(c => c.Parameter == u.Parameter))
        .Map(u => u.Parameter);

      parametersToAdd.Iter(p =>
      {
        correlations = correlations.AddCorrelation(p);
      });

      var correlationParameters = correlations.Map(c => c.Parameter);
      var updateParameters = update.Map(u => u.Parameter);

      update.Iter((i, u) =>
      {
        var indexCorrelation = correlations.FindIndex(c => c.Parameter == u.Parameter);
        var toUpdate = correlations[indexCorrelation].Correlations.ToArray();

        var targetParameters = updateParameters
          .Skip(i + 1)
          .Take(updateParameters.Count - i - 1)
          .ToArr();
        var targetIndices = targetParameters.Map(p => correlationParameters.IndexOf(p));

        targetIndices.Iter((i, j) => toUpdate[j - indexCorrelation - 1] = u.Correlations[i]);

        var correlation = (u.Parameter, toUpdate.ToArr());
        correlations = correlations.SetItem(indexCorrelation, correlation);
      });

      return correlations;
    }

    internal static Arr<(string Parameter, Arr<double> Correlations)> CorrelationsFor(
      this Arr<(string Parameter, Arr<double> Correlations)> correlations,
      Arr<string> parameters
    )
    {
      var parametersToRemove = correlations
        .Filter(c => !parameters.Exists(p => p == c.Parameter))
        .Map(c => c.Parameter);

      parametersToRemove.Iter(p =>
      {
        correlations = correlations.RemoveCorrelation(p);
      });

      var parametersToAdd = parameters
        .Filter(p => !correlations.Exists(c => c.Parameter == p));

      parametersToAdd.Iter(p =>
      {
        correlations = correlations.AddCorrelation(p);
      });

      return correlations;
    }

    internal static Arr<(string Parameter, Arr<double> Correlations)> AddCorrelation(
      this Arr<(string Parameter, Arr<double> Correlations)> correlations,
      string parameter
      )
    {
      RequireFalse(correlations.Exists(c => c.Parameter == parameter));

      var parameters = correlations.Map(c => c.Parameter) + parameter;
      parameters = parameters.OrderBy(n => n.ToUpperInvariant()).ToArr();

      var nParameters = parameters.Count;
      var indexInserted = parameters.IndexOf(parameter);

      correlations = parameters
        .Map((i, p) => correlations
          .Find(c => c.Parameter == p)
          .Match(
            c => i < indexInserted
              ? (c.Parameter, c.Correlations.Insert(indexInserted - i - 1, 0d))
              : c,
            () => (p, Repeat(0d, nParameters - i - 1).ToArr())
            )
          )
        .ToArr();

      return correlations;
    }

    internal static Arr<(string Parameter, Arr<double> Correlations)> RemoveCorrelation(
      this Arr<(string Parameter, Arr<double> Correlations)> correlations,
      string parameter
    )
    {
      var indexToRemove = correlations.FindIndex(c => c.Parameter == parameter);

      RequireTrue(indexToRemove.IsFound());

      correlations = correlations.RemoveAt(indexToRemove);

      if (0 == indexToRemove) return correlations;

      return correlations
        .Map((i, c) =>
        {
          if (i >= indexToRemove) return c;

          return (c.Parameter, c.Correlations.RemoveAt(indexToRemove - i - 1));
        })
        .ToArr();
    }

    internal static string ToDirectoryName(this DateTime dateTime) =>
      dateTime.ToString("yyyyMMddHHmmss", InvariantCulture);

    internal static DateTime FromDirectoryName(this string directoryName) =>
      DateTime.ParseExact(directoryName, "yyyyMMddHHmmss", InvariantCulture);

    internal static string GetDescription(this SamplingDesign samplingDesign) =>
      GetDescription(
        samplingDesign.DesignParameters,
        samplingDesign.LatinHypercubeDesign,
        samplingDesign.RankCorrelationDesign,
        samplingDesign.Seed,
        samplingDesign.Samples,
        samplingDesign.NoDataIndices
        );

    internal static string GetDescription(
      Arr<DesignParameter> designParameters,
      LatinHypercubeDesign latinHypercubeDesign,
      RankCorrelationDesign rankCorrelationDesign,
      int? seed,
      DataTable samples,
      Arr<int> noDataIndices
      )
    {
      var parts = new List<string>();

      if (samples.Rows.Count > 0)
      {
        parts.Add($"n = {samples.Rows.Count}");
      }

      if (seed.HasValue)
      {
        parts.Add($"seed = {seed.Value}");
      }

      if (noDataIndices.Count > 0)
      {
        parts.Add($"faults = {noDataIndices.Count}");
      }

      if (latinHypercubeDesign.LatinHypercubeDesignType != LatinHypercubeDesignType.None)
      {
        parts.Add($"LHS = {latinHypercubeDesign.LatinHypercubeDesignType}");
      }

      if (rankCorrelationDesign.RankCorrelationDesignType != RankCorrelationDesignType.None)
      {
        parts.Add($"RC = {rankCorrelationDesign.RankCorrelationDesignType}");
      }

      if (designParameters.Count > 0)
      {
        parts.AddRange(designParameters.Map(
          dp => dp.Distribution.ToString(dp.Name)
          ));
      }

      return Join(", ", parts);
    }

    internal static Option<DesignDigest> GetDesignDigest(this SamplingDesigns samplingDesigns, DateTime createdOn) =>
      samplingDesigns.DesignDigests.Find(dd => dd.CreatedOn == createdOn);
  }
}
