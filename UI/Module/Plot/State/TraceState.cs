using RVisUI.Model.Extensions;
using System.ComponentModel;

namespace Plot
{
  public class TraceState : INotifyPropertyChanged
  {
    public TraceState()
    {
    }

    public bool IsWorkingSetPanelOpen
    {
      get => _isWorkingSetPanelOpen;
      set => this.RaiseAndSetIfChanged(ref _isWorkingSetPanelOpen, value, PropertyChanged);
    }
    private bool _isWorkingSetPanelOpen;

    public event PropertyChangedEventHandler? PropertyChanged;
  }
}
