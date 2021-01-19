using System;
using System.Windows.Input;

#nullable disable

namespace Estimation.Design
{
  internal class IterationOptionsViewModel : IIterationOptionsViewModel
  {
    public string IterationsToAddText { get => "1234"; set => throw new NotImplementedException(); }
    public int? IterationsToAdd => throw new NotImplementedException();
    public string TargetAcceptRateText { get => "5678"; set => throw new NotImplementedException(); }
    public double? TargetAcceptRate => throw new NotImplementedException();
    public bool UseApproximation { get => true; set => throw new NotImplementedException(); }

    public ICommand OK => throw new NotImplementedException();

    public ICommand Cancel => throw new NotImplementedException();

    public bool? DialogResult { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
  }
}
