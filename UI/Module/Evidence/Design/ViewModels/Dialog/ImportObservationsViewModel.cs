using LanguageExt;
using System;
using System.Linq;
using System.Windows.Input;
using static LanguageExt.Prelude;

#nullable disable

namespace Evidence.Design
{
  internal sealed class ImportObservationsViewModel : IImportObservationsViewModel
  {
    public string SelectedFile { get => "C:\\dir\\...\\data.csv C:\\dir\\...\\data.csv C:\\dir\\...\\data.csv C:\\dir\\...\\data.csv C:\\dir\\...\\data.csv C:\\dir\\...\\data.csv C:\\dir\\...\\data.csv"; set => throw new NotImplementedException(); }
    public ICommand SelectFile => throw new NotImplementedException();
    public string FQIndependentVariable => "time [s]";
    public string EvidenceName { get => "evidence name"; set => throw new NotImplementedException(); }
    public string EvidenceDescription { get => "evidence description"; set => throw new NotImplementedException(); }
    public string RefName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public string RefHash { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public Arr<IObservationsColumnViewModel> ObservationsColumnViewModels
    {
      get => Range(1, 30)
        .Map(
          i => new ObservationsColumnViewModel(
            i == 1 ? "iv-subject" : $"subject{i + 1:0000}",
            i == 1 ? Array("iv-subject") : Array(ObservationsColumnViewModel.NO_SELECTION, Range(i, 3).Map(j => $"subject{i + 1:0000}").ToArray()),
            Range(i, i * 2).Map(j => 2.0 * j).ToArr()
            )
          )
        .ToArr<IObservationsColumnViewModel>();
      set => throw new NotImplementedException();
    }
    public string ErrorMessage { get => null; set => throw new NotImplementedException(); }
    public ICommand OK => throw new NotImplementedException();
    public ICommand Cancel => throw new NotImplementedException();
    public bool? DialogResult { get => default; set => throw new NotImplementedException(); }
  }
}
