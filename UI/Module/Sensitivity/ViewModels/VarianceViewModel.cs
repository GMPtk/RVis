using LanguageExt;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using ReactiveUI;
using RVis.Base.Extensions;
using RVis.Model;
using RVis.Model.Extensions;
using RVisUI.AppInf.Extensions;
using RVisUI.Model;
using RVisUI.Model.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using static RVis.Base.Check;
using static RVis.Base.Extensions.NumExt;
using static Sensitivity.MeasuresOps;
using static System.Double;
using DataTable = System.Data.DataTable;

namespace Sensitivity
{
  internal sealed class VarianceViewModel : ViewModelBase, IVarianceViewModel
  {
    internal VarianceViewModel(
      IAppState appState,
      IAppService appService,
      IAppSettings appSettings,
      ModuleState moduleState,
      SensitivityDesigns sensitivityDesigns
      )
    {
      _appState = appState;
      _appService = appService;
      _appSettings = appSettings;
      _moduleState = moduleState;
      _sensitivityDesigns = sensitivityDesigns;

      _simulation = appState.Target.AssertSome();

      ConfigurePlotModel();

      _reactiveSafeInvoke = appService.GetReactiveSafeInvoke();

      _subscriptions = new CompositeDisposable(

        moduleState.ObservableForProperty(ms => ms.SensitivityDesign).Subscribe(
          ObserveModuleStateSensitivityDesign
          ),

        moduleState.MeasuresState.ObservableForProperty(ms => ms.OutputMeasures).Subscribe(
          ObserveMeasuresStateOutputMeasures
          ),

        moduleState.MeasuresState.ObservableForProperty(ms => ms.SelectedOutputName).Subscribe(
          ObserveMeasuresStateSelectedOutputName
          ),

        this.ObservableForProperty(vm => vm.SelectedOutputName).Subscribe(
          ObserveSelectedOutputName
          ),

        this.ObservableForProperty(vm => vm.VarianceMeasureType).Subscribe(
          ObserveVarianceMeasureType
          ),

        _appSettings
          .GetWhenPropertyChanged()
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<string>(ObserveAppSettingsPropertyChange)
            )

        );

      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        Populate();
      }
    }

    public bool IsVisible
    {
      get => _isVisible;
      set => this.RaiseAndSetIfChanged(ref _isVisible, value);
    }
    private bool _isVisible;

    public bool IsSelected
    {
      get => _isSelected;
      set => this.RaiseAndSetIfChanged(ref _isSelected, value);
    }
    private bool _isSelected;

    public Arr<string> OutputNames
    {
      get => _outputNames;
      set => this.RaiseAndSetIfChanged(ref _outputNames, value);
    }
    private Arr<string> _outputNames;

    public int SelectedOutputName
    {
      get => _selectedOutputName;
      set => this.RaiseAndSetIfChanged(ref _selectedOutputName, value);
    }
    private int _selectedOutputName = NOT_FOUND;

    public VarianceMeasureType VarianceMeasureType
    {
      get => _varianceMeasureType;
      set => this.RaiseAndSetIfChanged(ref _varianceMeasureType, value);
    }
    private VarianceMeasureType _varianceMeasureType = VarianceMeasureType.None;

    public PlotModel PlotModel
    {
      get => _plotModel;
      set => this.RaiseAndSetIfChanged(ref _plotModel, value);
    }
    private PlotModel _plotModel;

    public override void HandleCancelTask()
    {
      _cancellationTokenSource?.Cancel();
    }

    protected override void Dispose(bool disposing)
    {
      if (!_disposed)
      {
        if (disposing)
        {
          _subscriptions.Dispose();
          if (_cancellationTokenSource?.IsCancellationRequested == false)
          {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
          }
        }

        _disposed = true;
      }

      base.Dispose(disposing);
    }

    private void ObserveModuleStateSensitivityDesign(object _)
    {
      if (_moduleState.SensitivityDesign == default)
      {
        using (_reactiveSafeInvoke.SuspendedReactivity)
        {
          _cancellationTokenSource?.Cancel();
          PlotModel.Series.Clear();
          OutputNames = default;
          SelectedOutputName = NOT_FOUND;
          VarianceMeasureType = VarianceMeasureType.None;

          IsVisible = false;
        }
      }
    }

    private void ObserveMeasuresStateOutputMeasures(object _)
    {
      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        Populate();
      }
    }

    private void ObserveMeasuresStateSelectedOutputName(object _)
    {
      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        Populate();
      }
    }

    private async Task GenerateOutputMeasuresAsync(string outputName, ServerLicense serverLicense)
    {
      using (serverLicense)
      {
        _cancellationTokenSource = new CancellationTokenSource();

        TaskName = "Generate Output Measures";
        IsRunningTask = true;
        CanCancelTask = true;

        try
        {
          var measures = await Task.Run(
            () => GenerateOutputMeasures(
              outputName,
              _moduleState.SensitivityDesign.SerializedDesign,
              _moduleState.SensitivityDesign.Samples,
              _moduleState.DesignOutputs,
              serverLicense.Client,
              _cancellationTokenSource.Token,
              s => _appService.ScheduleLowPriorityAction(() => RaiseTaskMessageEvent(s))
            ),
            _cancellationTokenSource.Token
            );

          _moduleState.MeasuresState.SelectedOutputName = outputName;

          _moduleState.MeasuresState.OutputMeasures =
            _moduleState.MeasuresState.OutputMeasures.Add(outputName, measures);

          _sensitivityDesigns.SaveOutputMeasures(
            _moduleState.SensitivityDesign,
            outputName,
            measures.FirstOrder,
            measures.TotalOrder,
            measures.Variance
            );

          var nansInFirstOrder = measures.FirstOrder.Rows
            .Cast<DataRow>()
            .Skip(1)
            .Any(dr => dr.ItemArray.OfType<double>().Any(IsNaN));

          var nansInTotalOrder = measures.TotalOrder.Rows
            .Cast<DataRow>()
            .Skip(1)
            .Any(dr => dr.ItemArray.OfType<double>().Any(IsNaN));

          if (nansInFirstOrder || nansInTotalOrder)
          {
            _appState.Status = "NaN(s) generated by sensitivity::tell()";
          }
        }
        catch (OperationCanceledException)
        {
          // expected
        }
        catch (Exception ex)
        {
          _appService.Notify(
            nameof(VarianceViewModel),
            nameof(GenerateOutputMeasuresAsync),
            ex
            );
        }

        IsRunningTask = false;
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = default;
      }
    }

    private void ObserveSelectedOutputName(object _)
    {
      if (!_reactiveSafeInvoke.React) return;

      var outputName = OutputNames[SelectedOutputName];

      if (_moduleState.MeasuresState.OutputMeasures.ContainsKey(outputName))
      {
        _moduleState.MeasuresState.SelectedOutputName = outputName;
        return;
      }

      var loadedMeasures = _sensitivityDesigns.LoadOutputMeasures(
        _moduleState.SensitivityDesign,
        outputName,
        out (DataTable FirstOrder, DataTable TotalOrder, DataTable Variance) measures
        );

      if (loadedMeasures)
      {
        _moduleState.MeasuresState.OutputMeasures =
          _moduleState.MeasuresState.OutputMeasures.Add(outputName, measures);
        _moduleState.MeasuresState.SelectedOutputName = outputName;
        return;
      }

      if (_moduleState.DesignOutputs.IsEmpty)
      {
        _appService.Notify(
          NotificationType.Information,
          "Change Selected Output",
          outputName,
          "Cannot compute measures as design outputs are not loaded. Reload data to continue."
          );
        _moduleState.MeasuresState.SelectedOutputName = outputName;
        return;
      }

      void SomeServer(ServerLicense serverLicense)
      {
        var __ = GenerateOutputMeasuresAsync(outputName, serverLicense);
      }

      void NoServer()
      {
        _appService.Notify(
          NotificationType.Information,
          nameof(VarianceViewModel),
          nameof(ObserveSelectedOutputName),
          "No R server available."
          );
      }

      _appService.RVisServerPool.RequestServer().Match(SomeServer, NoServer);
    }

    private void ObserveVarianceMeasureType(object _)
    {
      if (!_reactiveSafeInvoke.React) return;

      SetVerticalAxisTitle();
      PopulateSeries();
      PlotModel.InvalidatePlot(true);
    }

    private void ObserveAppSettingsPropertyChange(string propertyName)
    {
      if (PlotModel == default) return;
      if (!propertyName.IsThemeProperty()) return;

      PlotModel.ApplyThemeToPlotModelAndAxes();
      PlotModel.InvalidatePlot(false);
    }

    private void Populate()
    {
      if (_moduleState.SensitivityDesign == default || _moduleState.MeasuresState.OutputMeasures.IsEmpty)
      {
        IsVisible = false;
        return;
      }

      var canPlot = _moduleState.MeasuresState.OutputMeasures.ContainsKey(
        _moduleState.MeasuresState.SelectedOutputName
        );

      canPlot = canPlot && _moduleState.Trace != default;

      if (!canPlot)
      {
        PlotModel.Series.Clear();
        PlotModel.InvalidatePlot(updateData: true);
        return;
      }

      if (0 == PlotModel.Series.Count)
      {
        var factors = _moduleState.SensitivityDesign.Samples.Columns
          .Cast<DataColumn>()
          .Select(c => c.ColumnName)
          .ToArr();

        foreach (var factor in factors)
        {
          PlotModel.Series.Add(new LineSeries { Title = factor, StrokeThickness = 2, Tag = factor });
        }
      }

      if (OutputNames.IsEmpty)
      {
        var outputNames = _moduleState.Trace.ColumnNames
          .Skip(1)
          .OrderBy(cn => cn.ToUpperInvariant())
          .ToArr();

        OutputNames = outputNames;
      }

      RequireFalse(OutputNames.IsEmpty);

      SelectedOutputName = OutputNames.IndexOf(_moduleState.MeasuresState.SelectedOutputName);

      if (VarianceMeasureType == VarianceMeasureType.None)
      {
        VarianceMeasureType = VarianceMeasureType.MainEffect;
      }

      SetVerticalAxisTitle();

      PopulateSeries();
      PlotModel.InvalidatePlot(true);

      if (!IsVisible)
      {
        IsVisible = true;
        IsSelected = true;
      }
    }

    private void ConfigurePlotModel()
    {
      var plotModel = new PlotModel
      {
        LegendPosition = LegendPosition.RightMiddle,
        LegendPlacement = LegendPlacement.Outside
      };

      plotModel.ApplyThemeToPlotModelAndAxes();

      var independentVariable = _simulation.SimConfig.SimOutput.IndependentVariable;

      plotModel.Axes.Add(new LinearAxis
      {
        Position = AxisPosition.Bottom,
        Title = independentVariable.GetFQName()
      });

      plotModel.Axes.Add(new LinearAxis
      {
        Position = AxisPosition.Left,
      });

      _plotModel = plotModel;
    }

    private void SetVerticalAxisTitle()
    {
      RequireNotNull(PlotModel);
      RequireFalse(VarianceMeasureType == VarianceMeasureType.None);

      PlotModel.GetAxis(AxisPosition.Left).Title = _varianceMeasureNames[VarianceMeasureType];
    }

    private void PopulateSeries()
    {
      RequireNotNull(PlotModel);
      RequireTrue(
        _moduleState.MeasuresState.OutputMeasures.ContainsKey(
          _moduleState.MeasuresState.SelectedOutputName
          )
        );

      var (firstOrder, totalOrder, variance) = _moduleState.MeasuresState.OutputMeasures[
        _moduleState.MeasuresState.SelectedOutputName
        ];

      DataTable seriesSource;

      switch (VarianceMeasureType)
      {
        case VarianceMeasureType.MainEffect:
          seriesSource = firstOrder;
          break;

        case VarianceMeasureType.TotalEffect:
          seriesSource = totalOrder;
          break;

        case VarianceMeasureType.Variance:
          seriesSource = variance;
          break;

        default: throw new InvalidOperationException(nameof(VarianceMeasureType));
      }

      foreach (LineSeries lineSeries in PlotModel.Series)
      {
        lineSeries.Points.Clear();

        var factor = lineSeries.Tag as string;

        for (var row = 0; row < seriesSource.Rows.Count; ++row)
        {
          var dataRow = seriesSource.Rows[row];
          var x = dataRow.Field<double>(0);
          var y = dataRow.Field<double>(factor);
          lineSeries.Points.Add(new DataPoint(x, y));
        }
      }
    }

    private static readonly IDictionary<VarianceMeasureType, string> _varianceMeasureNames = new SortedDictionary<VarianceMeasureType, string>
    {
      [VarianceMeasureType.MainEffect] = "Main Effect",
      [VarianceMeasureType.TotalEffect] = "Total Effect",
      [VarianceMeasureType.Variance] = "Variance"
    };

    private readonly IAppState _appState;
    private readonly IAppService _appService;
    private readonly IAppSettings _appSettings;
    private readonly ModuleState _moduleState;
    private readonly SensitivityDesigns _sensitivityDesigns;
    private readonly Simulation _simulation;
    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
    private readonly IDisposable _subscriptions;
    private CancellationTokenSource _cancellationTokenSource;
    private bool _disposed = false;
  }
}
