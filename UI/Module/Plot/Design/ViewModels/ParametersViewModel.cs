using System;
using System.Collections.ObjectModel;
using System.Linq;
using static System.Linq.Enumerable;
using static System.String;

namespace Plot.Design
{
  internal class ParametersViewModel : IParametersViewModel
  {
    public ObservableCollection<IParameterViewModel> UnselectedParameters
    {
      get => new ObservableCollection<IParameterViewModel>(
        Range(1, 40).Select(i => new ParameterViewModel(
          i % 2 == 0 ? $"Unselected{i:0000}" : Join(" ", Repeat($"Unselected{i:0000}", i * 2)),
          i.ToString(),
          i % 2 == 0 ? $"u{i:0000}" : Join(string.Empty, Repeat($"u{i:0000}", i * 2)),
          Data.LorumIpsum,
          i.ToString(),
          i,
          false
          )));
      set => throw new NotImplementedException();
    }

    public ObservableCollection<IParameterViewModel> SelectedParameters
    {
      get => new ObservableCollection<IParameterViewModel>(
        Range(1, 20).Select(i => new ParameterViewModel(
          i % 2 == 0 ? $"Selected{i:0000}" : Join(" ", Repeat($"Selected{i:0000}", i * 2)),
          i.ToString(),
          i % 2 == 0 ? $"u{i:0000}" : Join(string.Empty, Repeat($"u{i:0000}", i * 2)),
          Data.LorumIpsum,
          i.ToString(),
          i % 3 == 0 ? default(double?) : i,
          true
          )));
      set => throw new NotImplementedException();
    }
  }
}
