using LanguageExt;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using ReactiveUI;
using RVis.Base.Extensions;
using RVis.Data;
using RVis.Model;
using RVis.Model.Extensions;
using RVisUI.AppInf.Extensions;
using RVisUI.Model;
using RVisUI.Model.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using static RVis.Base.Check;

namespace Sampling
{
  internal class OutputsDesignActivityViewModel : DesignActivityViewModelBase, IOutputsDesignActivityViewModel, INotifyPropertyChanged, IDisposable
  {
    internal OutputsDesignActivityViewModel(IAppService appService, IAppSettings appSettings, SimOutput simOutput, ModuleState moduleState)
      : base(appService, appSettings, "AWAITING OUTPUTS")
    {
      _simOutput = simOutput;
      _moduleState = moduleState;

      _elementNames = _simOutput.DependentVariables
        .Map(e => e.Name)
        .OrderBy(n => n.ToUpperInvariant())
        .ToArr();

      _selectedElementName = _moduleState.DesignState.SelectedElementName ?? ElementNames.Head();

      _outputs = CreatePlotModel(_simOutput);
      _outputs.ApplyThemeToPlotModelAndAxes();

      ConfigureVerticalAxis(_outputs, _selectedElementName, _simOutput);

      this
        .ObservableForProperty(vm => vm.SelectedElementName)
        .Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<object>(
            ObserveSelectedElementName
          )
        );
    }

    internal OutputsDesignActivityViewModel(IAppService appService, IAppSettings appSettings)
      : base(appService, appSettings, "AWAITING OUTPUTS")
    {
      RequireTrue(Splat.ModeDetector.InDesignMode());
    }

    public Arr<string> ElementNames
    {
      get => _elementNames;
      set => this.RaiseAndSetIfChanged(ref _elementNames, value, PropertyChanged);
    }
    private Arr<string> _elementNames;

    public string SelectedElementName
    {
      get => _selectedElementName;
      set => this.RaiseAndSetIfChanged(ref _selectedElementName, value, PropertyChanged);
    }
    private string _selectedElementName;

    public PlotModel Outputs
    {
      get => _outputs;
      set => this.RaiseAndSetIfChanged(ref _outputs, value, PropertyChanged);
    }
    private PlotModel _outputs;

    public event PropertyChangedEventHandler PropertyChanged;

    internal void Initialize(int nOutputs)
    {
      _outputs.DefaultColors = OxyPalettes.Gray(nOutputs).Colors;
    }

    internal void AddOutput(int index, NumDataTable output)
    {
      RequireFalse(_outputMap.ContainsKey(index));

      _outputMap.Add(index, output);

      PlotOutput(Outputs, SelectedElementName, index, output);

      _outputs.ResetAllAxes();
      _outputs.InvalidatePlot(true);
    }

    internal void AddOutputs(Arr<(int Index, NumDataTable Output)> outputs)
    {
      foreach (var (index, output) in outputs)
      {
        RequireFalse(_outputMap.ContainsKey(index));
        _outputMap.Add(index, output);
        PlotOutput(Outputs, SelectedElementName, index, output);
      }

      _outputs.ResetAllAxes();
      _outputs.InvalidatePlot(true);
    }

    internal void Clear()
    {
      _outputMap.Clear();
      _outputs.Series.Clear();
      _outputs.InvalidatePlot(true);
    }

    private static PlotModel CreatePlotModel(SimOutput simOutput)
    {
      var outputs = new PlotModel();

      var independentVariable = simOutput.IndependentVariable;
      var horizontalAxis = new LinearAxis
      {
        Position = AxisPosition.Bottom,
        Title = independentVariable.Name,
        Unit = independentVariable.Unit
      };
      outputs.Axes.Add(horizontalAxis);

      var verticalAxis = new LinearAxis
      {
        Position = AxisPosition.Left
      };
      outputs.Axes.Add(verticalAxis);

      return outputs;
    }

    private static void PlotOutput(PlotModel plotModel, string dependentVariableName, int index, NumDataTable output)
    {
      var independentVariableName = plotModel.GetAxis(AxisPosition.Bottom).Title;
      var independentData = output[independentVariableName];
      var dependentData = output[dependentVariableName];

      var series = new LineSeries()
      {
        Tag = index,
        StrokeThickness = 1,
        LineStyle = LineStyle.Solid
      };

      for (var row = 0; row < independentData.Length; ++row)
      {
        var x = independentData[row];
        var y = dependentData[row];
        var point = new DataPoint(x, y);
        series.Points.Add(point);
      }

      plotModel.Series.Add(series);
    }

    protected override void ObserveThemeChange()
    {
      if (_outputs != default)
      {
        _outputs.ApplyThemeToPlotModelAndAxes();
        _outputs.InvalidatePlot(false);
      }

      base.ObserveThemeChange();
    }

    private void ObserveSelectedElementName(object _)
    {
      _outputs.Series.Clear();

      if (SelectedElementName.IsAString())
      {
        ConfigureVerticalAxis(_outputs, SelectedElementName, _simOutput);

        foreach (var kvp in _outputMap)
        {
          PlotOutput(Outputs, SelectedElementName, kvp.Key, kvp.Value);
        }
      }

      _outputs.ResetAllAxes();
      _outputs.InvalidatePlot(true);

      _moduleState.DesignState.SelectedElementName = SelectedElementName;
    }

    private static void ConfigureVerticalAxis(PlotModel plotModel, string elementName, SimOutput simOutput)
    {
      var verticalAxis = plotModel.GetAxis(AxisPosition.Left);
      var simValue = simOutput.FindElement(elementName).AssertSome();
      verticalAxis.Title = simValue.Name;
      verticalAxis.Unit = simValue.Unit;
    }

    private readonly SimOutput _simOutput;
    private readonly ModuleState _moduleState;
    private readonly IDictionary<int, NumDataTable> _outputMap = new SortedDictionary<int, NumDataTable>();
  }
}
