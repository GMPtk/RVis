namespace Estimation.Design
{
  internal sealed class ViewModel : IViewModel
  {
    public IPriorsViewModel PriorsViewModel => new PriorsViewModel();

    public ILikelihoodViewModel LikelihoodViewModel => new LikelihoodViewModel();

    public IDesignViewModel DesignViewModel => new DesignViewModel();

    public ISimulationViewModel SimulationViewModel => new SimulationViewModel();

    public IPosteriorViewModel PosteriorViewModel => new PosteriorViewModel();

    public IFitViewModel FitViewModel => new FitViewModel();

    public IDesignDigestsViewModel DesignDigestsViewModel => new DesignDigestsViewModel();
  }
}
