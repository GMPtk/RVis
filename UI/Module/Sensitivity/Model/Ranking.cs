using LanguageExt;

namespace Sensitivity
{
  internal readonly struct Ranking
  {
    internal Ranking(
      double? xBegin,
      double? xEnd,
      Arr<string> outputs,
      Arr<(string Parameter, double Score, bool IsSelected)> parameters
      )
    {
      XBegin = xBegin;
      XEnd = xEnd;
      Outputs = outputs;
      Parameters = parameters;
    }

    internal double? XBegin { get; }
    internal double? XEnd { get; }
    internal Arr<string> Outputs { get; }
    internal Arr<(string Parameter, double Score, bool IsSelected)> Parameters { get; }
  }
}
