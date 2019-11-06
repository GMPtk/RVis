using LanguageExt;
using System;
using System.Data;
using System.Linq;
using static LanguageExt.Prelude;

namespace Estimation
{
  internal sealed partial class McmcChain : IDisposable
  {
    internal static string ToLLColumnName(string name) =>
      $"{name} {_logLikelihoodSuffix}";

    internal static string ToAcceptColumnName(string name) =>
      $"{name} {_acceptSuffix}";

    private static Arr<string> ToColumnNames(ModelParameter[] modelParameters)
    {
      var suffices = Array(
        string.Empty, // parameter value
        " " + _logLikelihoodSuffix,
        " " + _acceptSuffix
        );

      return modelParameters
        .Select(mp => mp.Name)
        .SelectMany(n => suffices.Map(s => $"{n}{s}"))
        .ToArr();
    }

    private static Arr<string> ToColumnNames(ModelOutput[] modelOutputs)
    {
      var suffices = Array(
        " " + _logLikelihoodSuffix,
        " " + _acceptSuffix
        );

      return modelOutputs
        .Select(mo => mo.Name)
        .SelectMany(n => suffices.Map(s => $"{n}{s}"))
        .ToArr();
    }

    public void Dispose() =>
      Dispose(disposing: true);

    private void Dispose(bool disposing)
    {
      if (!_disposed)
      {
        if (disposing)
        {
          _chainData.Dispose();
          _errorData.Dispose();
        }

        _disposed = true;
      }
    }

    private bool _disposed = false;
  }
}
