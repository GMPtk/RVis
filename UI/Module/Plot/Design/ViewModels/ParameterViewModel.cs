using System;
using System.Windows.Input;
using static Plot.Design.Data;

#nullable disable

namespace Plot.Design
{
  internal class ParameterViewModel : IParameterViewModel
  {
    public ParameterViewModel()
    {
      Name = "A parameter";
      DefaultValue = "4321";
      Unit = "p units";
      Description = LorumIpsum;
      TValue = "1234";
      NValue = 1234;
      IsSelected = false;
    }

    internal ParameterViewModel(string name, string defaultValue, string unit, string description, string tValue, double? nValue, bool isSelected)
    {
      Name = name;
      DefaultValue = defaultValue;
      Unit = unit;
      Description = description;
      TValue = tValue;
      NValue = nValue;
      IsSelected = isSelected;
    }

    public string Name { get; set; }

    public string SortKey => throw new NotImplementedException();

    public string DefaultValue { get; set; }

    public string Unit { get; set; }

    public string Description { get; set; }

    public bool IsSelected { get; set; }

    public ICommand ToggleSelect => throw new NotImplementedException();

    public string TValue { get; set; }

    public ICommand ResetValue => throw new NotImplementedException();

    public double? NValue { get; set; }
    public double Minimum { get => NValue.HasValue ? NValue.Value / 2 : default; set => throw new NotImplementedException(); }

    public ICommand IncreaseMinimum => throw new NotImplementedException();

    public bool CanIncreaseMinimum { get => false; set => throw new NotImplementedException(); }

    public ICommand DecreaseMinimum => throw new NotImplementedException();

    public double Maximum { get => NValue.HasValue ? NValue.Value * 2 : default; set => throw new NotImplementedException(); }

    public ICommand IncreaseMaximum => throw new NotImplementedException();

    public ICommand DecreaseMaximum => throw new NotImplementedException();

    public bool CanDecreaseMaximum { get => true; set => throw new NotImplementedException(); }
    public string Ticks { get => default; set => throw new NotImplementedException(); }

    public void Set(double value, double minimum, double maximum)
    {
      throw new NotImplementedException();
    }
  }
}
