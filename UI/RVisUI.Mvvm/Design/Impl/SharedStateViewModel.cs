using System;
using System.Windows.Input;
using static System.Linq.Enumerable;

#nullable disable

namespace RVisUI.Mvvm.Design
{
  public class SharedStateViewModel : ISharedStateViewModel
  {
    public ICommand OpenView => throw new NotImplementedException();

    public ICommand CloseView => throw new NotImplementedException();

    public bool IsViewOpen { get => true; set => throw new NotImplementedException(); }

    public object[][] SharedParameters
    {
      get => Range(1, 20).Select(i => new[] { null, null, $"p{i:00000000}", $"{i:0000} < {i + 1:0000} < {i + 2:0000}", $"u{i:0000}", $"ln(p{i}) ~ N({i*2},{i*3})" }).ToArray();
      set => throw new NotImplementedException();
    }
    public ICommand ApplyParametersState => throw new NotImplementedException();

    public ICommand ShareParametersState => throw new NotImplementedException();

    public object[][] SharedOutputs
    {
      get => Range(1, 20).Select(i => new[] { null,null, $"op{i:00000000}" }).ToArray();
      set => throw new NotImplementedException();
    }
    public ICommand ApplyOutputsState => throw new NotImplementedException();

    public ICommand ShareOutputsState => throw new NotImplementedException();

    public object[][] SharedObservations
    {
      get => Range(1, 20).Select(i => new[] { null, null, $"subject{i:0000} x {i} from source{i:0000} (refname{i:0000})" }).ToArray();
      set => throw new NotImplementedException();
    }
    public ICommand ApplyObservationsState => throw new NotImplementedException();

    public ICommand ShareObservationsState => throw new NotImplementedException();

    public ICommand ApplyState => throw new NotImplementedException();

    public ICommand ShareState => throw new NotImplementedException();
  }
}
