using LanguageExt;
using RVis.Data;
using RVis.Model;
using RVisUI.Model;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using static LanguageExt.Prelude;
using static System.Linq.Enumerable;
using static System.String;

#nullable disable

namespace Plot.Design
{
  public sealed class TraceViewModel : ITraceViewModel
  {
    public Arr<(string SeriesName, Arr<(string SerieName, NumDataTable Serie)> Series)> DataSet
    {
      get => Array(
        (Data.HPTorqueOverRPM.Name, Array((default(string), Data.HPTorqueOverRPM)))
        );
      set => throw new NotImplementedException();
    }

    public Arr<ITraceDataPlotViewModel> TraceDataPlotViewModels
    {
      get => ModuleState.DefaultTraceDataPlotStates
              .Map(s => new TraceDataPlotViewModel(s))
              .ToArr<ITraceDataPlotViewModel>();
      set => throw new NotImplementedException();
    }

    public int ChartGridLayout { get => 2; set => throw new NotImplementedException(); }

    public int IsWorkingSetPanelOpen { get => 0; set => throw new NotImplementedException(); }
    public ObservableCollection<IParameterViewModel> WorkingSet =>
      new ObservableCollection<IParameterViewModel>(
        Range(1, 20).Select(i => new ParameterViewModel(
          i % 2 == 0 ? $"Selected{i:0000}" : Join(" ", Repeat($"Selected{i:0000}", i * 2)),
          i.ToString(),
          i % 2 == 0 ? $"u{i:0000}" : Join(string.Empty, Repeat($"u{i:0000}", i * 2)),
          Data.LorumIpsum,
          i.ToString(),
          i,
          false
          ))
        );

    public bool IsSelected { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public Stck<SimInput> SessionEdits { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public ICommand UndoWorkingChange => throw new NotImplementedException();

    public ICommand PlotWorkingChanges => throw new NotImplementedException();

    public bool HasPendingWorkingChanges { get; set; }
  }
}
