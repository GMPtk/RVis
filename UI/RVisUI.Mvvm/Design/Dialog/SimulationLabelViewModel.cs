using System;
using System.Windows.Input;

namespace RVisUI.Mvvm.Design
{
  public class SimulationLabelViewModel : ISimulationLabelViewModel
  {
    public string? Name { get => "A name"; set => throw new NotImplementedException(); }
    public string? Description { get => "A description"; set => throw new NotImplementedException(); }

    public ICommand OK => throw new NotImplementedException();

    public ICommand Cancel => throw new NotImplementedException();

    public bool? DialogResult { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
  }
}
