namespace Sensitivity.Design
{
  internal sealed class ViewModel : IViewModel
  {
    public IParametersViewModel ParametersViewModel => new ParametersViewModel();

    public IDesignViewModel DesignViewModel => new DesignViewModel();

    public IMorrisMeasuresViewModel MorrisMeasuresViewModel => new MorrisMeasuresViewModel();

    public IMorrisEffectsViewModel MorrisEffectsViewModel => new MorrisEffectsViewModel();

    public IFast99MeasuresViewModel Fast99MeasuresViewModel => new Fast99MeasuresViewModel();

    public IFast99EffectsViewModel Fast99EffectsViewModel => new Fast99EffectsViewModel();

    public IDesignDigestsViewModel DesignDigestsViewModel => new DesignDigestsViewModel();
  }
}
