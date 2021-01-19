using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using static System.Linq.Enumerable;

#nullable disable

namespace Evidence.Design
{
  internal class ManageViewModel : IManageViewModel
  {
    public ObservableCollection<IEvidenceSourceViewModel> EvidenceSourceViewModels =>
      new ObservableCollection<IEvidenceSourceViewModel>
      (
        Range(1, 20).Select(i => new EvidenceSourceViewModel(i, $"name{i:0000}", $"description{i:0000}"))
      );

    public IEvidenceSourceViewModel SelectedEvidenceSourceViewModel { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public ICommand DeleteEvidenceSource => throw new NotImplementedException();

    public ObservableCollection<IObservationsViewModel> ObservationsViewModels
    {
      get => new ObservableCollection<IObservationsViewModel>
      (
        Range(1, 20).Select(i => new ObservationsViewModel(
          i,
          $"Subject{i:0000}",
          $"RefName{i:0000}",
          $"Source{i:0000}",
          Range(i, i * 2).Select(j => j * 2.0).ToArr(),
          Range(i, i * 2).Select(j => j * 3.0).ToArr()
          ))
      );
      set => throw new NotImplementedException();
    }

    public IObservationsViewModel SelectedObservationsViewModel { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public ICommand DeleteObservations => throw new NotImplementedException();
  }
}
