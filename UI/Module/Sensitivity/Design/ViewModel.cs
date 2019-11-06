namespace Sensitivity.Design
{
  internal sealed class ViewModel : IViewModel
  {
    public IParametersViewModel ParametersViewModel => new ParametersViewModel();

    public IDesignViewModel DesignViewModel => new DesignViewModel();

    public IVarianceViewModel VarianceViewModel => new VarianceViewModel();

    public IEffectsViewModel EffectsViewModel => new EffectsViewModel();

    public IDesignDigestsViewModel DesignDigestsViewModel => new DesignDigestsViewModel();
  }
}
