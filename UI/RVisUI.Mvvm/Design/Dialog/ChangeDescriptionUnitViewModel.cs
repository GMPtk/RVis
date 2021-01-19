using System;
using System.Windows.Input;
using static System.Linq.Enumerable;

#nullable disable

namespace RVisUI.Mvvm.Design
{
  public class ChangeDescriptionUnitViewModel : IChangeDescriptionUnitViewModel
  {
    public string TargetSymbol { get => "abc"; set => throw new NotImplementedException(); }
    public string Description { get => "def"; set => throw new NotImplementedException(); }
    public string Unit { get => "ghi"; set => throw new NotImplementedException(); }

    public object[][] LineSymDescUnit => Range(0, 20).Select(i => new object[] { i, $"Sym{i:00}", $"Desc desc desc desc desc desc desc desc desc desc desc desc desc desc desc desc desc{i:00}", $"Unit unit unit unit unit unit unit{i:00}" }).ToArray();

    public ICommand OK => throw new NotImplementedException();

    public ICommand Cancel => throw new NotImplementedException();

    public bool? DialogResult { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
  }
}
