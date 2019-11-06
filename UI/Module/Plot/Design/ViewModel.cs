namespace Plot.Design
{
  internal sealed class ViewModel : IViewModel
  {
    public ITraceViewModel TraceViewModel => new TraceViewModel();

    public IParametersViewModel ParametersViewModel => new ParametersViewModel();

    public IOutputsViewModel OutputsViewModel => new OutputsViewModel();
  }
}
