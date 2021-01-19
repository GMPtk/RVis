using LanguageExt;
using OxyPlot;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Evidence
{
  public interface IViewModel
  {
    IBrowseViewModel BrowseViewModel { get; }
    IManageViewModel ManageViewModel { get; }
    ICommand Import { get; }
  }

  public interface ISubjectViewModel
  {
    string Subject { get; }
    int NSelected { get; set; }
    int NAvailable { get; set; }
  }

  public interface IObservationsViewModel
  {
    int ID { get; }
    string Subject { get; }
    string RefName { get; }
    string Source { get; }
    string Data { get; }
    bool IsSelected { get; set; }
  }

  public interface IBrowseViewModel
  {
    PlotModel? PlotModel { get; set; }
    Arr<ISubjectViewModel> SubjectViewModels { get; set; }
    ISubjectViewModel? SelectedSubjectViewModel { get; set; }
    Arr<IObservationsViewModel> ObservationsViewModels { get; set; }
  }

  public interface IEvidenceSourceViewModel
  {
    int ID { get; }
    string Name { get; }
    string? Description { get; }
  }

  public interface IManageViewModel
  {
    ObservableCollection<IEvidenceSourceViewModel> EvidenceSourceViewModels { get; }
    IEvidenceSourceViewModel? SelectedEvidenceSourceViewModel { get; set; }
    ICommand DeleteEvidenceSource { get; }

    ObservableCollection<IObservationsViewModel>? ObservationsViewModels { get; set; }
    IObservationsViewModel? SelectedObservationsViewModel { get; set; }
    ICommand DeleteObservations { get; }
  }

  public interface IObservationsColumnViewModel
  {
    string ColumnName { get; }
    Arr<string> Subjects { get; }
    string Subject { get; set; }
    string? RefName { get; set; }
    Arr<double> Observations { get; }
    string Content { get; }
  }

  public interface IImportObservationsViewModel
  {
    string? SelectedFile { get; set; }
    ICommand SelectFile { get; }
    string? FQIndependentVariable { get; }
    string? EvidenceName { get; set; }
    string? EvidenceDescription { get; set; }
    string? RefName { get; set; }
    string? RefHash { get; set; }
    Arr<IObservationsColumnViewModel> ObservationsColumnViewModels { get; set; }
    string? ErrorMessage { get; set; }
    ICommand OK { get; }
    ICommand Cancel { get; }
    bool? DialogResult { get; set; }
  }
}
