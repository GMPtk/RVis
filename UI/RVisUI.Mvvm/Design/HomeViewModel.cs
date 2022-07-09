using System;

namespace RVisUI.Mvvm.Design
{
  public class HomeViewModel : IHomeViewModel
  {
    public ISelectSimulationViewModel SelectSimulationViewModel => new SelectSimulationViewModel();

    public IImportRSimViewModel ImportRSimViewModel => new ImportRSimViewModel();

    public IImportMCSimViewModel ImportMCSimViewModel => new ImportMCSimViewModel();

    public ILibraryViewModel LibraryViewModel => throw new NotImplementedException();

    public IRunControlViewModel RunControlViewModel => new RunControlViewModel();

    public IAcatHostViewModel AcatHostViewModel => new AcatHostViewModel();

    public int SelectedIndex { get => 2; set => throw new NotImplementedException(); }
  }
}
