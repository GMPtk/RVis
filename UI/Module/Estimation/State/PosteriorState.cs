namespace Estimation
{
  internal partial class PosteriorState
  {
    internal PosteriorState(int beginIteration, int endIteration)
    {
      BeginIteration = beginIteration;
      EndIteration = endIteration;
    }

    internal int BeginIteration { get; }
    internal int EndIteration { get; }
  }
}
