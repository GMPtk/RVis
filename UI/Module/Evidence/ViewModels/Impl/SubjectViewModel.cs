using RVisUI.Model.Extensions;
using System.ComponentModel;

namespace Evidence
{
  internal class SubjectViewModel : ISubjectViewModel, INotifyPropertyChanged
  {
    internal SubjectViewModel(string subject)
    {
      Subject = subject;
    }

    public string Subject { get; }

    public int NSelected
    {
      get => _nSelected;
      set => this.RaiseAndSetIfChanged(ref _nSelected, value, PropertyChanged);
    }
    private int _nSelected;

    public int NAvailable
    {
      get => _nAvailable;
      set => this.RaiseAndSetIfChanged(ref _nAvailable, value, PropertyChanged);
    }
    private int _nAvailable;

    public event PropertyChangedEventHandler? PropertyChanged;
  }
}
