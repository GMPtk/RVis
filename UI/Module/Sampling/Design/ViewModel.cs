namespace Sampling.Design
{
  internal sealed class ViewModel : IViewModel
  {
    public IParametersViewModel ParametersViewModel => new ParametersViewModel();

    public IDesignViewModel DesignViewModel => new DesignViewModel();

    public IDesignDigestsViewModel DesignDigestsViewModel => new DesignDigestsViewModel();
  }
}
