using LanguageExt;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using static System.Linq.Enumerable;
using static LanguageExt.Prelude;

#nullable disable

namespace RVisUI.Mvvm.Design
{
  public class ImportTmplViewModel : IImportTmplViewModel
  {
    public string FileName => "abc.R";

    public Arr<IParameterCandidateViewModel> ParameterCandidates => Array<IParameterCandidateViewModel>(
      new ParameterCandidateViewModel(false, "P1", 1.23, "unit", "Desc", null),
      new ParameterCandidateViewModel(false, "P2_withamuchlongernamethanP1", 4.56, "Very, very, very, very long unit", "Very, very, very, very, very, very, very, very, very, very long description", null)
      ).AddRange(
        Range(3, 18).Select(i => new ParameterCandidateViewModel(i % 2 == 1, $"P{i:00}", i, $"unit{i:00}", $"Desc{i:00}", null))
      );

    public ICommand UseAllParameters => throw new System.NotImplementedException();

    public ICommand UseNoParameters => throw new System.NotImplementedException();

    public IElementCandidateViewModel IndependentVariable
    {
      get => _elementCandidates[0];
      set => throw new System.NotImplementedException();
    }

    public ICommand SetIndependentVariable { get; }

    public ObservableCollection<IElementCandidateViewModel> ElementCandidates => 
      new ObservableCollection<IElementCandidateViewModel>(_elementCandidates);

    public IElementCandidateViewModel SelectedElementCandidate
    {
      get => _elementCandidates[2];
      set => throw new System.NotImplementedException();
    }

    public ICommand UseAllOutputs => throw new System.NotImplementedException();

    public ICommand UseNoOutputs => throw new System.NotImplementedException();

    public string SimulationName { get => "SimFolderWithALongLongName"; set => throw new System.NotImplementedException(); }

    public string SimulationDescription { get => "Not too long a description, if you please..."; set => throw new System.NotImplementedException(); }

    public ICommand OK => throw new System.NotImplementedException();

    public ICommand Cancel => throw new System.NotImplementedException();

    public bool? DialogResult { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

    private readonly IElementCandidateViewModel[] _elementCandidates = new[]
      {
        new ElementCandidateViewModel(false, "E1", "value01", false, new [] { 1.23 }, null, null, null),
        new ElementCandidateViewModel(false, "E2_withamuchlongernamethanP1", "value02", false, new [] { 4.56 }, "Very, very, very, very long unit", "Very, very, very, very, very, very, very, very, very, very long description", null),
      }.Concat(Range(3, 18).Select(i => new ElementCandidateViewModel(i % 2 == 1, $"E{i:00}", $"value{i:00}", false, new double[] { i }, $"unit{i:00}", $"Desc{i:00}", null))).ToArray();
  }
}
