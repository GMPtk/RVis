using RVisUI.Model.Extensions;
using System.ComponentModel;

namespace Estimation
{
  internal sealed class DesignState : INotifyPropertyChanged
  {
    internal int? Iterations
    {
      get => _iterations;
      set => this.RaiseAndSetIfChanged(ref _iterations, value, PropertyChanged);
    }
    private int? _iterations;

    internal int? BurnIn
    {
      get => _burnIn;
      set => this.RaiseAndSetIfChanged(ref _burnIn, value, PropertyChanged);
    }
    private int? _burnIn;

    internal int? Chains
    {
      get => _chains;
      set => this.RaiseAndSetIfChanged(ref _chains, value, PropertyChanged);
    }
    private int? _chains;

    public event PropertyChangedEventHandler? PropertyChanged;
  }
}
