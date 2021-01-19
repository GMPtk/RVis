using Nett;
using System.IO;
using static RVis.Base.Check;
using static System.IO.Path;

namespace Estimation
{
  internal partial class PosteriorState
  {
    internal static void Save(PosteriorState posteriorState, string pathToEstimationDesign)
    {
      RequireDirectory(pathToEstimationDesign);

      var pathToPosteriorState = Combine(pathToEstimationDesign, $"{nameof(PosteriorState).ToLowerInvariant()}.toml");

      SavePosteriorState(posteriorState, pathToPosteriorState);
    }

    internal static PosteriorState? Load(string pathToEstimationDesign)
    {
      RequireDirectory(pathToEstimationDesign);

      var pathToPosteriorState = Combine(pathToEstimationDesign, $"{nameof(PosteriorState).ToLowerInvariant()}.toml");

      if (!File.Exists(pathToPosteriorState)) return default;

      return LoadPosteriorState(pathToPosteriorState);
    }

    private class _PosteriorStateDTO
    {
      public int BeginIteration { get; set; }
      public int EndIteration { get; set; }
    }

    private static void SavePosteriorState(PosteriorState posteriorState, string pathToPosteriorState)
    {
      var dto = new _PosteriorStateDTO
      {
        BeginIteration = posteriorState.BeginIteration,
        EndIteration = posteriorState.EndIteration
      };

      Toml.WriteFile(dto, pathToPosteriorState);
    }

    private static PosteriorState LoadPosteriorState(string pathToPosteriorState)
    {
      var dto = Toml.ReadFile<_PosteriorStateDTO>(pathToPosteriorState);

      return new PosteriorState(dto.BeginIteration, dto.EndIteration);
    }
  }
}
