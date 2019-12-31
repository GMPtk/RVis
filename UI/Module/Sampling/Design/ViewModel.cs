namespace Sampling.Design
{
  internal sealed class ViewModel : IViewModel
  {
    public IParametersViewModel ParametersViewModel => new ParametersViewModel();

    public ISamplesViewModel SamplesViewModel => new SamplesViewModel();

    public IDesignViewModel DesignViewModel => new DesignViewModel();

    public IOutputsViewModel OutputsViewModel => new OutputsViewModel();

    public IDesignDigestsViewModel DesignDigestsViewModel => new DesignDigestsViewModel();
  }
}
