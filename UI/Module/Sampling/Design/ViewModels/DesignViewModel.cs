using System;
using System.Windows.Input;

namespace Sampling.Design
{
  internal sealed class DesignViewModel : IDesignViewModel
  {
    public bool IsSelected { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public DateTime? CreatedOn => DateTime.Now;

    public ICommand CreateDesign => throw new NotImplementedException();

    public bool CanCreateDesign => throw new NotImplementedException();

    public ICommand UnloadDesign => throw new NotImplementedException();

    public bool CanUnloadDesign => throw new NotImplementedException();

    public double AcquireOutputsProgress => 25d;

    public int NOutputsAcquired => 250;

    public int NOutputsToAcquire => 1000;

    public ICommand AcquireOutputs => throw new NotImplementedException();

    public bool CanAcquireOutputs => throw new NotImplementedException();

    public ICommand CancelAcquireOutputs => throw new NotImplementedException();

    public bool CanCancelAcquireOutputs => throw new NotImplementedException();
  }
}
