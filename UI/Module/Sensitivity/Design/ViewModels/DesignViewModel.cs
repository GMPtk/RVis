using LanguageExt;
using RVis.Data;
using System;
using System.Data;
using System.Windows.Input;
using static LanguageExt.Prelude;

namespace Sensitivity.Design
{
  internal sealed class DesignViewModel : IDesignViewModel
  {
    public Arr<string> Factors => Range(1, 30).Map(i => $"Var{i:000} ~ dist{i:000}(dist params...)").ToArr();

    public Arr<string> Invariants => Range(1, 30).Map(i => $"Var{i:000} = {i}").ToArr();

    public SensitivityMethod SensitivityMethod 
    { 
      get => SensitivityMethod.Morris; 
      set => throw new NotImplementedException(); 
    }

    public int? NoOfRuns { get => 123; set => throw new NotImplementedException(); }

    public int? NoOfSamples { get => 456; set => throw new NotImplementedException(); }

    public ICommand CreateDesign => throw new NotImplementedException();

    public bool CanCreateDesign => true;

    public DateTime? DesignCreatedOn => DateTime.Now;

    public ICommand UnloadDesign => throw new NotImplementedException();

    public bool CanUnloadDesign => false;

    public double AcquireOutputsProgress => 0.333;

    public int NOutputsAcquired => 456;

    public int NOutputsToAcquire => 789;

    public ICommand AcquireOutputs => throw new NotImplementedException();

    public bool CanAcquireOutputs => true;

    public ICommand CancelAcquireOutputs => throw new NotImplementedException();

    public bool CanCancelAcquireOutputs => false;

    public DataView Inputs => Data.Samples;

    public int SelectedInputIndex { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public ICommand ShareParameters => throw new NotImplementedException();

    public bool CanShareParameters => throw new NotImplementedException();

    public ICommand ViewError => throw new NotImplementedException();

    public bool CanViewError => throw new NotImplementedException();

    public bool ShowIssues { get => true; set => throw new NotImplementedException(); }

    public bool HasIssues => false;

    public bool IsSelected { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
  }
}
