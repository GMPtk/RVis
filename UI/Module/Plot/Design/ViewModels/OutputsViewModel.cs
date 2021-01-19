using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using static System.Linq.Enumerable;

#nullable disable

namespace Plot.Design
{
  internal sealed class OutputsViewModel : IOutputsViewModel
  {
    public ObservableCollection<ILogEntryViewModel> LogEntryViewModels =>
      new ObservableCollection<ILogEntryViewModel>(
        Range(1, 100).Select(i => new LogEntryViewModel
        {
          EnteredOn = DateTime.Now.AddDays(i).ToString("o"),
          RequesterTypeName = $"req{i}",
          ParameterAssignments = $"xyz={i:00000000}"
        })
      );

    public ILogEntryViewModel[] SelectedLogEntryViewModels { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public ICommand LoadLogEntry => throw new NotImplementedException();

    public ICommand CreateOutputGroup => throw new NotImplementedException();

    public ICommand FollowKeyboardInLogEntries => throw new NotImplementedException();

    public ObservableCollection<IOutputGroupViewModel> OutputGroupViewModels =>
      new ObservableCollection<IOutputGroupViewModel>(
        Range(1, 100).Select(i => new OutputGroupViewModel
        {
          CreatedOn = DateTime.Now.AddDays(i).ToString("o"),
          Name = $"name{i}",
          Description = $"desc={i:00000000}"
        })
      );

    public IOutputGroupViewModel SelectedOutputGroupViewModel { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public ICommand LoadOutputGroup => throw new NotImplementedException();

    public ICommand FollowKeyboardInOutputGroups => throw new NotImplementedException();
  }
}
