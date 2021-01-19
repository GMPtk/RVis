using System;
using System.Windows.Input;

namespace Sampling
{
  internal sealed class RCParameterDiagonalViewModel : IRCParameterViewModel
  {
    internal RCParameterDiagonalViewModel(string name, double correlation)
    {
      Name = name;
      _correlationN = correlation;
    }

    public string Name { get; }

    public ICommand SetKeyboardTarget { get; } = null!;

    public double? CorrelationN
    {
      get => _correlationN;
      set => throw new InvalidOperationException(nameof(CorrelationN));
    }
    
    public string? CorrelationT 
    { 
      get => throw new InvalidOperationException(nameof(CorrelationT));
      set => throw new InvalidOperationException(nameof(CorrelationT));
    }

    private readonly double? _correlationN;
  }
}
