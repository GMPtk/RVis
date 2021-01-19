using LanguageExt;
using Plot.Controls.Views;
using RVis.Base.Extensions;
using RVis.Data;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Plot.Controls
{
  internal sealed class ChartGrid : UserControl
  {
    public ChartGrid()
    {
      _grid = new Grid();
      Content = _grid;
    }

    public Arr<(string SeriesName, Arr<(string SerieName, NumDataTable Serie)> Series)> DataSet
    {
      get => (Arr<(string SeriesName, Arr<(string SerieName, NumDataTable Serie)> Series)>)GetValue(DataSetProperty);
      set => SetValue(DataSetProperty, value);
    }

    public static readonly DependencyProperty DataSetProperty =
      DependencyProperty.Register(
        nameof(DataSet),
        typeof(Arr<(string SeriesName, Arr<(string SerieName, NumDataTable Serie)> Series)>),
        typeof(ChartGrid),
        new PropertyMetadata(default(Arr<(string SeriesName, Arr<(string SerieName, NumDataTable Serie)> Series)>), HandleDataSetChanged)
        );

    private static void HandleDataSetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (!d.Resolve(out ChartGrid? chartGrid)) return;
      ConfigureGrid(chartGrid);
    }

    public Arr<ITraceDataPlotViewModel> TraceDataPlotViewModels
    {
      get => (Arr<ITraceDataPlotViewModel>)GetValue(TraceDataPlotViewModelsProperty);
      set => SetValue(TraceDataPlotViewModelsProperty, value);
    }

    public static readonly DependencyProperty TraceDataPlotViewModelsProperty =
      DependencyProperty.Register(
        nameof(TraceDataPlotViewModels),
        typeof(Arr<ITraceDataPlotViewModel>),
        typeof(ChartGrid),
        new PropertyMetadata(default(Arr<ITraceDataPlotViewModel>), HandleTraceDataPlotViewModelsChanged)
        );

    private static void HandleTraceDataPlotViewModelsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (!d.Resolve(out ChartGrid? chartGrid)) return;
      ConfigureGrid(chartGrid);
    }

    public int Layout
    {
      get => (int)GetValue(LayoutProperty);
      set => SetValue(LayoutProperty, value);
    }

    public static readonly DependencyProperty LayoutProperty =
      DependencyProperty.Register(
        nameof(Layout),
        typeof(int),
        typeof(ChartGrid),
        new PropertyMetadata(default(int), HandleLayoutChanged)
        );

    private static void HandleLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (!d.Resolve(out ChartGrid? chartGrid)) return;
      ConfigureGrid(chartGrid);
    }

    private static void ConfigureGrid(ChartGrid chartGrid)
    {
      var grid = chartGrid._grid;

      // disconnect vms from outgoing views else Oxy will complain
      grid.Children.OfType<TraceDataPlotView>().Iter(v => v.DataContext = default);

      grid.Children.Clear();
      grid.RowDefinitions.Clear();

      var dataSet = chartGrid.DataSet;
      if (dataSet.IsEmpty) return;

      var traceDataPlotViewModels = chartGrid.TraceDataPlotViewModels;
      if (traceDataPlotViewModels.IsEmpty) return;

      traceDataPlotViewModels.Iter(vm => { vm.DataSet = dataSet; });

      var visibleViewModels = traceDataPlotViewModels.Filter(vm => vm.State.IsVisible);

      if (visibleViewModels.IsEmpty) return;

      var anyHeightsUnknown = visibleViewModels.Exists(vm => vm.State.ViewHeight <= UNIT_HEIGHT);
      if (anyHeightsUnknown)
      {
        var knownHeights = traceDataPlotViewModels
          .Filter(vm => vm.State.ViewHeight > UNIT_HEIGHT)
          .Map(vm => vm.State.ViewHeight);

        var averageHeight = knownHeights.IsEmpty ? UNIT_HEIGHT : knownHeights.Average();

        traceDataPlotViewModels
          .Filter(vm => vm.State.ViewHeight <= UNIT_HEIGHT)
          .Iter(vm => vm.State.ViewHeight = averageHeight);
      }

      var viewModel = visibleViewModels.Head();

      grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(viewModel.State.ViewHeight, GridUnitType.Star) });

      var view = new TraceDataPlotView();
      Grid.SetRow(view, 0);
      grid.Children.Add(view);
      view.DataContext = viewModel;

      for (var i = 1; i < visibleViewModels.Count; ++i)
      {
        viewModel = visibleViewModels[i];

        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(5, GridUnitType.Pixel) });

        var gridSplitter = new GridSplitter { Height = 5, HorizontalAlignment = HorizontalAlignment.Stretch };
        Grid.SetRow(gridSplitter, i * 2 - 1);
        grid.Children.Add(gridSplitter);

        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(viewModel.State.ViewHeight, GridUnitType.Star) });

        view = new TraceDataPlotView();
        Grid.SetRow(view, i * 2);
        grid.Children.Add(view);
        view.DataContext = viewModel;

        gridSplitter.DragCompleted += HandleGridSplitterDragCompleted;
      }
    }

    private static void HandleGridSplitterDragCompleted(object sender, DragCompletedEventArgs e)
    {
      if ((sender as GridSplitter)?.Parent is not Grid grid) return;

      grid.Children.OfType<TraceDataPlotView>().Iter(v =>
      {
        var viewModel = (ITraceDataPlotViewModel)v.DataContext;
        viewModel.State.ViewHeight = Math.Round(v.ActualHeight);
      });
    }

    private const double UNIT_HEIGHT = 1.0;

    private readonly Grid _grid;
  }
}
