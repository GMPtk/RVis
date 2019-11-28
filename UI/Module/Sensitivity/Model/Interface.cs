using LanguageExt;

namespace Sensitivity
{
  internal interface IScorer
  {
    Arr<(string ParameterName, double Score)> GetScores(double from, double to, Arr<string> outputs);
  }
}
