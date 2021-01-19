using LanguageExt;
using RVisUI.Model.Extensions;
using System.ComponentModel;

namespace Sampling
{
  internal sealed class OutputsState : INotifyPropertyChanged
  {
    internal string? SelectedOutputName
    {
      get => _selectedOutputName;
      set => this.RaiseAndSetIfChanged(ref _selectedOutputName, value, PropertyChanged);
    }
    private string? _selectedOutputName;

    public bool IsSeriesTypeLine
    {
      get => _isSeriesTypeLine;
      set => this.RaiseAndSetIfChanged(ref _isSeriesTypeLine, value, PropertyChanged);
    }
    private bool _isSeriesTypeLine;

    public Arr<string> ObservationsReferences
    {
      get => _observationsReferences;
      set => this.RaiseAndSetIfChanged(ref _observationsReferences, value, PropertyChanged);
    }
    private Arr<string> _observationsReferences;

    public event PropertyChangedEventHandler? PropertyChanged;
  }
}
