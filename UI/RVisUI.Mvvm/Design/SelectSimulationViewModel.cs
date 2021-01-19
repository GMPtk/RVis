using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

#nullable disable

namespace RVisUI.Mvvm.Design
{
  public class SelectSimulationViewModel : ISelectSimulationViewModel
  {
    public ObservableCollection<ISimulationViewModel> SimulationVMs =>
      //null;
      new ObservableCollection<ISimulationViewModel>(
        Enumerable.Range(1, 100).Select(i => new SimulationViewModel
        {
          Title = $"Name {i:0000}",
          Description = $"Desc {i:0000} which might be very, very, very, very, very, very, very long",
          DirectoryName = $"File{i:0000}.R"
        })
      );

    public ISimulationViewModel SelectedSimulationVM { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public string PathToLibrary { get => @"c:\xxx\yyy\zzz"; set => throw new NotImplementedException(); }

    public ICommand OpenSimulation => throw new NotImplementedException();

    public ICommand DeleteSimulation => throw new NotImplementedException();

    public string RVersion { get => "3.4"; set => throw new NotImplementedException(); }
  }
}
