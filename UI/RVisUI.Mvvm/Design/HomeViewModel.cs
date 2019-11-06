using System;

namespace RVisUI.Mvvm.Design
{
  public class HomeViewModel : IHomeViewModel
  {
    public ISelectSimulationViewModel SelectSimulationViewModel => new SelectSimulationViewModel();

    public IImportSimulationViewModel ImportSimulationViewModel => throw new NotImplementedException();

    public ILibraryViewModel LibraryViewModel => throw new NotImplementedException();
  }
}
