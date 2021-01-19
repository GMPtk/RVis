using LanguageExt;
using RVisUI.Model.Extensions;
using System.ComponentModel;
using System.Linq;
using static System.Linq.Enumerable;
using static System.String;

namespace Estimation
{
  internal sealed class ObservationsViewModel : IObservationsViewModel, INotifyPropertyChanged
  {
    internal ObservationsViewModel(
      int id,
      string subject,
      string refName,
      string source,
      Arr<double> x,
      Arr<double> y
      )
    {
      ID = id;
      Subject = subject;
      RefName = refName;
      Source = source;
      Data = Join(
        "  ",
        Range(0, x.Count).Select(i => $"({x[i]:G4},{y[i]:G4})")
        );
    }

    public int ID { get; }

    public string Subject { get; }

    public string RefName { get; }

    public string Source { get; }

    public string Data { get; }

    public bool IsSelected
    {
      get => _isSelected;
      set => this.RaiseAndSetIfChanged(ref _isSelected, value, PropertyChanged);
    }
    private bool _isSelected;

    public event PropertyChangedEventHandler? PropertyChanged;
  }
}
