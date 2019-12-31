﻿using LanguageExt;
using RVis.Data;
using System;
using System.Windows.Input;
using static LanguageExt.Prelude;

namespace Sensitivity.Design
{
  internal sealed class MorrisEffectsViewModel : IMorrisEffectsViewModel
  {
    public MorrisEffectsViewModel()
    {
      var muStarSigmaViewModel = new MuStarSigmaViewModel();
      muStarSigmaViewModel.SetBounds(-2, 15, -2, 15);
      muStarSigmaViewModel.Plot(
        Range(1, 10)
          .Map(i => new MuStarSigmaParameterMeasure($"{i}", i * 1d, i * 1d))
          .ToArr()
        );
      MuStarSigmaViewModel = muStarSigmaViewModel;

      var traceViewModel = new TraceViewModel();
      traceViewModel.PlotTraceData(
        new NumDataColumn("time", new[] { 0.0, 1.0, 2.0, 3.0 }),
        new NumDataColumn("output", new[] { 0.0, 4.0, 8.0, 12.0 })
        );
      traceViewModel.SelectedX = 2.0;
      TraceViewModel = traceViewModel;
    }

    public bool IsVisible => true;

    public bool IsReady { get => true; set => throw new NotImplementedException(); }

    public IMuStarSigmaViewModel MuStarSigmaViewModel { get; }

    public ITraceViewModel TraceViewModel { get; }

    public Arr<string> OutputNames => Range(1, 20).Map(i => $"name{i:000}").ToArr();

    public bool CanSelectOutputName => throw new NotImplementedException();

    public int SelectedOutputName { get => 3; set => throw new NotImplementedException(); }

    public int PlaySpeed => 4;

    public ICommand PlaySimulation => throw new NotImplementedException();

    public bool CanPlaySimulation => throw new NotImplementedException();

    public ICommand StopSimulation => throw new NotImplementedException();

    public bool CanStopSimulation => throw new NotImplementedException();

    public ICommand PlaySlower => throw new NotImplementedException();

    public bool CanPlaySlower => throw new NotImplementedException();

    public ICommand PlayFaster => throw new NotImplementedException();

    public bool CanPlayFaster => throw new NotImplementedException();

    public string XUnits => "secs.secs.secs.secs.secs";

    public Arr<IRankedParameterViewModel> RankedParameterViewModels =>
      Range(1, 20)
        .Map(i => new RankedParameterViewModel($"parameter {i:000}", i) { IsSelected = i % 2 != 0 })
        .ToArr<IRankedParameterViewModel>();

    public Arr<string> RankedUsing => Range(1, 20).Map(i => $"output {i:000}").ToArr();

    public double? RankedFrom => 5.4321;

    public double? RankedTo => 9.8765;

    public ICommand UseRankedParameters => throw new NotImplementedException();

    public ICommand ShareRankedParameters => throw new NotImplementedException();
  }
}
