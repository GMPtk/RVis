using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using static LanguageExt.Prelude;

namespace Sampling.Design
{
  internal sealed class OutputsFilteredSamplesViewModel : IOutputsFilteredSamplesViewModel
  {
    public string IndependentVariableName => "t";
    public int? IndependentVariableIndex => -1;
    public double? IndependentVariableValue => 123.4;
    public string? IndependentVariableUnit => "hr";

    public string? OutputName => "AnOutputWithALongName";

    public double? FromN { get => 234.5; set => throw new NotImplementedException(); }
    public string? FromT { get => "345.6"; set => throw new NotImplementedException(); }

    public double? ToN { get => 456.7; set => throw new NotImplementedException(); }
    public string? ToT { get => "567.8"; set => throw new NotImplementedException(); }

    public ICommand AddNewFilter => throw new NotImplementedException();

    public ObservableCollection<IOutputsFilterViewModel> OutputsFilterViewModels =>
      new ObservableCollection<IOutputsFilterViewModel>(
        Range(1, 100).Map(i => new OutputsFilterViewModel(
          $"IV{i:0000}",
          i + 1,
          $"IVUnit{i:0000}",
          $"Output{i:0000}",
          i + 2,
          i + 3,
          $"OutputUnit{i:0000}",
          default!,
          default!
          )
        { IsEnabled = i % 2 == 0 })
      );

    public bool IsUnion { get => true; set => throw new NotImplementedException(); }
    public bool IsEnabled { get => true; set => throw new NotImplementedException(); }
  }
}
