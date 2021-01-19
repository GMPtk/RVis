using LanguageExt;
using RVis.Data;
using RVisUI.Model.Extensions;
using System.ComponentModel;

namespace Sampling
{
  internal sealed class FilteredSamplesState : INotifyPropertyChanged
  {
    internal bool IsEnabled
    {
      get => _isEnabled;
      set => this.RaiseAndSetIfChanged(ref _isEnabled, value, PropertyChanged);
    }
    private bool _isEnabled;

    internal bool IsUnion
    {
      get => _isUnion;
      set => this.RaiseAndSetIfChanged(ref _isUnion, value, PropertyChanged);
    }
    private bool _isUnion;

    public event PropertyChangedEventHandler? PropertyChanged;
  }
}
