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
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reactive.Disposables;
using System.Windows.Input;
using static RVis.Base.Check;
using static RVis.Base.Extensions.NumExt;
using static RVis.Base.Extensions.LangExt;

namespace Sampling
{
  internal sealed class OutputsViewModel : IOutputsViewModel, INotifyPropertyChanged, IDisposable
  {
    internal OutputsViewModel(IAppState appState, IAppService appService, IAppSettings appSettings, ModuleState moduleState)
    {
      _appState = appState;
      _moduleState = moduleState;
      _simulation = appState.Target.AssertSome();

      var output = _simulation.SimConfig.SimOutput;

      _outputNames = output.DependentVariables
        .Map(e => e.Name)
        .OrderBy(n => n.ToUpperInvariant())
        .ToArr();

      _selectedOutputName = _outputNames.IndexOf(
        _moduleState.OutputsState.SelectedOutputName ?? _outputNames.Head()
        );

      Outputs = CreatePlotModel(output.IndependentVariable);
      Outputs.MouseDown += HandleOutputsMouseDown;
      Outputs.DefaultColors = OxyPalettes.Gray(_moduleState.Samples?.Rows.Count ?? 1).Colors;
      Outputs.ApplyThemeToPlotModelAndAxes();

      PopulateVerticalAxis();
      PopulateOutputs();

      ToggleSeriesType = ReactiveCommand.Create(HandleToggleSeriesType);
      ResetAxes = ReactiveCommand.Create(HandleResetAxes);

      ShareParameterValues = ReactiveCommand.Create(
        HandleShareParameterValues,
        this.ObservableForProperty(
          vm => vm.SelectedSample,
          _ => SelectedSample != NOT_FOUND
          )
        );

      _reactiveSafeInvoke = appService.GetReactiveSafeInvoke();

      _subscriptions = new CompositeDisposable(

        appSettings
          .GetWhenPropertyChanged()
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<string>(
              ObserveAppSettingsPropertyChange
              )
            ),

        moduleState.OutputsState
          .ObservableForProperty(os => os.SelectedOutputName)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveOutputsStateSelectedOutputName
              )
            ),

        moduleState
          .ObservableForProperty(ms => ms.Samples)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveModuleStateSamples
              )
            ),

        moduleState
          .ObservableForProperty(ms => ms.Outputs)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveModuleStateOutputs
              )
            ),

        this
          .ObservableForProperty(vm => vm.SelectedOutputName)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveSelectedOutputName
              )
            ),

        this
          .ObservableForProperty(vm => vm.SelectedSample)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveSelectedSample
              )
            )

        );
    }

    public bool IsVisible
    {
      get => _isVisible;
      set => this.RaiseAndSetIfChanged(ref _isVisible, value, PropertyChanged);
    }
    private bool _isVisible;

    public Arr<string> OutputNames
    {
      get => _outputNames;
      set => this.RaiseAndSetIfChanged(ref _outputNames, value, PropertyChanged);
    }
    private Arr<string> _outputNames;

    public int SelectedOutputName
    {
      get => _selectedOutputName;
      set => this.RaiseAndSetIfChanged(ref _selectedOutputName, value, PropertyChanged);
    }
    private int _selectedOutputName;

    public PlotModel Outputs { get; }

    public ICommand ToggleSeriesType { get; }

    public bool IsSeriesTypeLine
    {
      get => _isSeriesTypeLine;
      set => this.RaiseAndSetIfChanged(ref _isSeriesTypeLine, value, PropertyChanged);
    }
    private bool _isSeriesTypeLine;

    public ICommand ResetAxes { get; }

    public int SelectedSample
    {
      get => _selectedSample;
      set => this.RaiseAndSetIfChanged(ref _selectedSample, value, PropertyChanged);
    }
    private int _selectedSample = NOT_FOUND;

    public string SampleIdentifier
    {
      get => _sampleIdentifier;
      set => this.RaiseAndSetIfChanged(ref _sampleIdentifier, value, PropertyChanged);
    }
    private string _sampleIdentifier;

    public Arr<string> ParameterValues
    {
      get => _parameterValues;
      set => this.RaiseAndSetIfChanged(ref _parameterValues, value, PropertyChanged);
    }
    private Arr<string> _parameterValues;

    public ICommand ShareParameterValues { get; }

    public event PropertyChangedEventHandler PropertyChanged;

    public void Dispose() =>
      Dispose(true);

    private void HandleToggleSeriesType()
    {
      _moduleState.OutputsState.IsSeriesTypeLine = !IsSeriesTypeLine;
      IsSeriesTypeLine = _moduleState.OutputsState.IsSeriesTypeLine;

      Outputs.Series.Clear();
      PopulateOutputs();
      Outputs.InvalidatePlot(updateData: true);
    }

    private void HandleResetAxes()
    {
      Outputs.ResetAllAxes();
      Outputs.InvalidatePlot(updateData: false);
    }

    private void HandleOutputsMouseDown(object sender, OxyMouseDownEventArgs e)
    {
      var series = Outputs.GetSeriesFromPoint(e.Position);
      SelectedSample = series == default
        ? NOT_FOUND
        : RequireInstanceOf<int>(series.Tag);
    }

    private void HandleShareParameterValues()
    {
      var defaultInput = _simulation.SimConfig.SimInput;

      var row = _moduleState.SamplingDesign.Samples.Rows[SelectedSample];

      var parameterStates = _moduleState.SamplingDesign.DesignParameters.Map(dp =>
      {
        var (_, minimum, maximum) = _appState.SimSharedState.ParameterSharedStates.GetParameterValueStateOrDefaults(
          dp.Name,
          defaultInput.SimParameters
          );

        var value = row.Field<double>(dp.Name);
        if (value < minimum) minimum = value.GetPreviousOrderOfMagnitude();
        if (value > maximum) maximum = value.GetNextOrderOfMagnitude();

        return (dp.Name, value, minimum, maximum, NoneOf<IDistribution>());
      });

      _appState.SimSharedState.ShareParameterState(parameterStates);
    }

    private void ObserveAppSettingsPropertyChange(string propertyName)
    {
      if (!propertyName.IsThemeProperty()) return;

      Outputs.ApplyThemeToPlotModelAndAxes();
      Outputs.InvalidatePlot(updateData: false);
    }

    internal void ObserveOutputsStateSelectedOutputName(object _)
    {
      SelectedOutputName = _outputNames.IndexOf(_moduleState.OutputsState.SelectedOutputName);

      PopulateVerticalAxis();

      Outputs.Series.Clear();
      PopulateOutputs();
      Outputs.ResetAllAxes();
      Outputs.InvalidatePlot(updateData: true);
    }

    internal void ObserveModuleStateSamples(object _)
    {
      Outputs.DefaultColors = OxyPalettes.Gray(_moduleState.Samples?.Rows.Count ?? 1).Colors;
    }

    internal void ObserveModuleStateOutputs(object _)
    {
      PopulateVerticalAxis();

      if (_moduleState.Outputs.IsEmpty)
      {
        Outputs.Series.Clear();
      }

      PopulateOutputs();
      Outputs.ResetAllAxes();
      Outputs.InvalidatePlot(updateData: true);
      
      SelectedSample = NOT_FOUND;
      PopulateSelectedSample();
    }

    private void ObserveSelectedOutputName(object _)
    {
      PopulateVerticalAxis();

      Outputs.Series.Clear();
      PopulateOutputs();
      Outputs.ResetAllAxes();
      Outputs.InvalidatePlot(updateData: true);

      _moduleState.OutputsState.SelectedOutputName = _selectedOutputName.IsFound()
        ? _outputNames[_selectedOutputName]
        : default;
    }

    private void ObserveSelectedSample(object _) => 
      PopulateSelectedSample();

    private void PopulateVerticalAxis()
    {
      var outputName = _selectedOutputName.IsFound()
        ? _outputNames[_selectedOutputName]
        : default;

      var simValue = outputName.IsAString()
        ? _simulation.SimConfig.SimOutput.FindElement(outputName).AssertSome()
        : default;

      var verticalAxis = Outputs.GetAxis(AxisPosition.Left);
      verticalAxis.Title = simValue.Name;
      verticalAxis.Unit = simValue.Unit;
    }

    private void PopulateOutputs()
    {
      var outputName = _selectedOutputName.IsFound()
        ? _outputNames[_selectedOutputName]
        : default;

      if (outputName.IsAString())
      {
        var independentVariableName = _simulation.SimConfig.SimOutput.IndependentVariable.Name;

        _moduleState.Outputs.Iter(t =>
        {
          var isPlotted = Outputs.Series.Any(s => RequireInstanceOf<int>(s.Tag) == t.Index);
          if (isPlotted) return;

          var independentData = t.Output[independentVariableName];
          var dependentData = t.Output[outputName];

          Series series;
          var seriesTitle = $"#{t.Index + 1}";

          if (IsSeriesTypeLine)
          {
            var lineSeries = new LineSeries()
            {
              Title = seriesTitle,
              StrokeThickness = 1,
              LineStyle = LineStyle.Solid, 
              Color = Outputs.DefaultColors[Outputs.Series.Count % Outputs.DefaultColors.Count],
              Tag = t.Index
            };

            for (var row = 0; row < independentData.Length; ++row)
            {
              var x = independentData[row];
              var y = dependentData[row];
              var point = new DataPoint(x, y);
              lineSeries.Points.Add(point);
            }

            series = lineSeries;
          }
          else
          {
            var scatterSeries = new ScatterSeries()
            {
              Title = seriesTitle,
              MarkerType = MarkerType.Plus,
              MarkerStroke = Outputs.DefaultColors[Outputs.Series.Count % Outputs.DefaultColors.Count],
              MarkerSize = 1,
              Tag = t.Index
            };

            for (var row = 0; row < independentData.Length; ++row)
            {
              var x = independentData[row];
              var y = dependentData[row];
              var point = new ScatterPoint(x, y);
              scatterSeries.Points.Add(point);
            }

            series = scatterSeries;
          }

          Outputs.Series.Add(series);
        });
      }
    }

    private void PopulateSelectedSample()
    {
      if (SelectedSample == NOT_FOUND)
      {
        SampleIdentifier = default;
        ParameterValues = default;
        return;
      }

      SampleIdentifier = $"Sample #{SelectedSample + 1}";

      var row = _moduleState.SamplingDesign.Samples.Rows[SelectedSample];
      ParameterValues = _moduleState.SamplingDesign.DesignParameters
        .Map(dp => _simulation.SimConfig.SimInput.SimParameters.GetParameter(dp.Name))
        .Map(p => $"{p.Name} = {row.Field<double>(p.Name)} {p.Unit}")
        .ToArr();
    }

    private void Dispose(bool disposing)
    {
      if (!_disposed)
      {
        if (disposing)
        {
          _subscriptions.Dispose();
        }

        _disposed = true;
      }
    }

    private static PlotModel CreatePlotModel(SimElement independentVariable)
    {
      var outputs = new PlotModel
      {
        IsLegendVisible = false
      };

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

    private readonly IAppState _appState;
    private readonly ModuleState _moduleState;
    private readonly Simulation _simulation;
    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
    private readonly IDisposable _subscriptions;
    private bool _disposed = false;
  }
}
