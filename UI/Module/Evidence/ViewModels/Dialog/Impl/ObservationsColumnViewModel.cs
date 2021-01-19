using LanguageExt;
using RVis.Base.Extensions;
using RVisUI.Model.Extensions;
using System.ComponentModel;
using System.Linq;
using static RVis.Base.Check;
using static System.String;

namespace Evidence
{
  internal class ObservationsColumnViewModel : IObservationsColumnViewModel, INotifyPropertyChanged
  {
    internal const string NO_SELECTION = "<Ignore>";

    internal ObservationsColumnViewModel(string columnName, Arr<string> subjects, Arr<double> observations)
    {
      RequireTrue(columnName.IsAString());
      RequireFalse(subjects.IsEmpty);
      RequireFalse(observations.IsEmpty);

      ColumnName = columnName;

      Subjects = subjects;

      _subject = subjects.Count == 1
        ? subjects.Head()
        : subjects.Contains(columnName) 
          ? columnName 
          : NO_SELECTION;

      Observations = observations;

      Content = Join("  ", observations);      
    }

    public string ColumnName { get; }

    public Arr<string> Subjects { get; }

    public string Subject
    {
      get => _subject;
      set => this.RaiseAndSetIfChanged(ref _subject, value, PropertyChanged);
    }
    private string _subject;

    public string? RefName
    {
      get => _refName;
      set => this.RaiseAndSetIfChanged(ref _refName, value, PropertyChanged);
    }
    private string? _refName;

    public Arr<double> Observations { get; }

    public string Content { get; }

    public event PropertyChangedEventHandler? PropertyChanged;
  }
}
