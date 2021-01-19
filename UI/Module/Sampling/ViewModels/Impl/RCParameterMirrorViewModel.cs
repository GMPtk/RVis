using ReactiveUI;
using RVisUI.Model.Extensions;
using System;
using System.ComponentModel;
using System.Windows.Input;

namespace Sampling
{
  internal sealed class RCParameterMirrorViewModel : IRCParameterViewModel, INotifyPropertyChanged
  {
    internal RCParameterMirrorViewModel(IRCParameterViewModel toMirror)
    {
      Name = toMirror.Name;
      _toMirror = toMirror;

      toMirror
        .WhenAnyValue(vm => vm.CorrelationN)
        .Subscribe(ObserveMirrorCorrelationN);
    }

    public string Name { get; }

    public ICommand SetKeyboardTarget { get; } = null!;

    public double? CorrelationN
    {
      get => _correlationN;
      set => this.RaiseAndSetIfChanged(ref _correlationN, value, PropertyChanged);
    }
    private double? _correlationN;

    public string? CorrelationT
    {
      get => throw new InvalidOperationException(nameof(CorrelationT));
      set => throw new InvalidOperationException(nameof(CorrelationT));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void ObserveMirrorCorrelationN(double? _) =>
      CorrelationN = _toMirror.CorrelationN;

    private readonly IRCParameterViewModel _toMirror;
  }
}
