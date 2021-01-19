using LanguageExt;
using OxyPlot;
using OxyPlot.Annotations;
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
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reactive.Disposables;
using System.Windows.Input;
using static LanguageExt.Prelude;
using static RVis.Base.Check;
using static RVis.Base.Extensions.NumExt;
using static System.Math;
using DataTable = System.Data.DataTable;
using static System.Globalization.CultureInfo;
using OxyPlot.Legends;

namespace Sensitivity
{
  internal sealed class MorrisMeasuresViewModel : IMorrisMeasuresViewModel, INotifyPropertyChanged, IDisposable
  {
    internal MorrisMeasuresViewModel(
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
      var independentVariable = _simulation.SimConfig.SimOutput.IndependentVariable;
      XUnits = independentVariable.Unit;

      RankParameters = ReactiveCommand.Create(
        HandleRankParameters,
        this.ObservableForProperty(vm => vm.IsReady, _ => IsReady)
        );

      UseRankedParameters = ReactiveCommand.Create(
        HandleUseRankedParameters,
        this.ObservableForProperty(vm => vm.RankedParameterViewModels, _ => RankedParameterViewModels.Count > 0)
        );

      ShareRankedParameters = ReactiveCommand.Create(
        HandleShareRankedParameters,
        this.ObservableForProperty(vm => vm.RankedParameterViewModels, _ => RankedParameterViewModels.Count > 0)
        );

      PlotModel = new PlotModel();

      PlotModel.Legends.Add(new Legend
      {
        LegendPosition = LegendPosition.RightMiddle,
        LegendPlacement = LegendPlacement.Outside
      });

      PlotModel.Axes.Add(new LinearAxis
      {
        Position = AxisPosition.Bottom,
        Title = independentVariable.GetFQName()
      });

      PlotModel.Axes.Add(new LinearAxis
      {
        Position = AxisPosition.Left,
      });

      _annotation = new RectangleAnnotation
      {
        Fill = OxyColor.FromAColor(120, OxyColors.SkyBlue),
        MinimumX = 0,
        MaximumX = 0
      };
      PlotModel.Annotations.Add(_annotation);

#pragma warning disable CS0618 // Type or member is obsolete
      PlotModel.MouseDown += HandlePlotModelMouseDown;
      PlotModel.MouseMove += HandlePlotModelMouseMove;
      PlotModel.MouseUp += HandlePlotModelMouseUp;
#pragma warning restore CS0618 // Type or member is obsolete

      PlotModel.ApplyThemeToPlotModelAndAxes();

      _reactiveSafeInvoke = appService.GetReactiveSafeInvoke();

      _subscriptions = new CompositeDisposable(

        moduleState
          .ObservableForProperty(ms => ms.SensitivityDesign)
          .Subscribe(ObserveModuleStateSensitivityDesign),

        moduleState.MeasuresState
          .ObservableForProperty(ms => ms.MorrisOutputMeasures)
          .Subscribe(ObserveMeasuresStateMorrisOutputMeasures),

        moduleState.MeasuresState
          .ObservableForProperty(ms => ms.SelectedOutputName)
          .Subscribe(ObserveMeasuresStateSelectedOutputName),

        moduleState
          .ObservableForProperty(ms => ms.Ranking)
          .Subscribe(ObserveModuleStateRanking),

        this
          .ObservableForProperty(vm => vm.IsVisible)
          .Subscribe(ObserveIsVisible),

        this
          .ObservableForProperty(vm => vm.SelectedOutputName)
          .Subscribe(ObserveSelectedOutputName),

        this
          .ObservableForProperty(vm => vm.MorrisMeasureType)
          .Subscribe(ObserveMorrisMeasureType),

        this
          .ObservableForProperty(vm => vm.XBeginText)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveXBeginText
              )
            ),

        this
          .ObservableForProperty(vm => vm.XEndText)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveXEndText
              )
            ),

        _appSettings
          .GetWhenPropertyChanged()
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<string?>(
              ObserveAppSettingsPropertyChange
              )
            )

        );

      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        Populate();
        PlotModel.InvalidatePlot(updateData: true);
      }
    }

    public bool IsVisible
    {
      get => _isVisible;
      set => this.RaiseAndSetIfChanged(ref _isVisible, value, PropertyChanged);
    }
    private bool _isVisible;

    public bool IsReady
    {
      get => _isReady;
      set => this.RaiseAndSetIfChanged(ref _isReady, value, PropertyChanged);
    }
    private bool _isReady;

    public bool IsSelected
    {
      get => _isSelected;
      set => this.RaiseAndSetIfChanged(ref _isSelected, value, PropertyChanged);
    }
    private bool _isSelected;

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
    private int _selectedOutputName = NOT_FOUND;

    public MorrisMeasureType MorrisMeasureType
    {
      get => _morrisMeasureType;
      set => this.RaiseAndSetIfChanged(ref _morrisMeasureType, value, PropertyChanged);
    }
    private MorrisMeasureType _morrisMeasureType = MorrisMeasureType.None;

    public string? XUnits { get; }

    public string? XBeginText
    {
      get => _xBeginText;
      set => this.RaiseAndSetIfChanged(ref _xBeginText, value.CheckParseValue<double>(), PropertyChanged);
    }
    private string? _xBeginText;

    public double? XBegin =>
      double.TryParse(_xBeginText, out double d) ? d : default(double?);

    public string? XEndText
    {
      get => _xEndText;
      set => this.RaiseAndSetIfChanged(ref _xEndText, value.CheckParseValue<double>(), PropertyChanged);
    }
    private string? _xEndText;

    public double? XEnd =>
      double.TryParse(_xEndText, out double d) ? d : default(double?);

    public Arr<IRankedParameterViewModel> RankedParameterViewModels
    {
      get => _rankedParameterViewModels;
      set => this.RaiseAndSetIfChanged(ref _rankedParameterViewModels, value, PropertyChanged);
    }
    private Arr<IRankedParameterViewModel> _rankedParameterViewModels;

    public Arr<string> RankedUsing
    {
      get => _rankedUsing;
      set => this.RaiseAndSetIfChanged(ref _rankedUsing, value, PropertyChanged);
    }
    private Arr<string> _rankedUsing;

    public double? RankedFrom
    {
      get => _rankedFrom;
      set => this.RaiseAndSetIfChanged(ref _rankedFrom, value, PropertyChanged);
    }
    private double? _rankedFrom;

    public double? RankedTo
    {
      get => _rankedTo;
      set => this.RaiseAndSetIfChanged(ref _rankedTo, value, PropertyChanged);
    }
    private double? _rankedTo;

    public ICommand RankParameters { get; }

    public ICommand UseRankedParameters { get; }

    public ICommand ShareRankedParameters { get; }

    public PlotModel PlotModel { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Dispose() =>
      Dispose(disposing: true);

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

    private void HandleRankParameters()
    {
      var outputs = _moduleState.MeasuresState.MorrisOutputMeasures.Keys
        .Select(k => (Output: k, IsSelected: _moduleState.Ranking.Outputs.Contains(k)))
        .ToArr();

      var parameters = _moduleState.Ranking.Parameters
        .Filter(p => p.IsSelected)
        .Map(p => p.Parameter);

      var scorer = new MorrisScorer(
        _moduleState.MeasuresState.MorrisOutputMeasures
        );

      var rankingViewModel = new RankingViewModel(
        XBegin,
        XEnd,
        XUnits,
        outputs,
        parameters,
        scorer
        );

      var didOK = _appService.ShowDialog(
        new RankingDialog(),
        rankingViewModel,
        default
        );

      if (didOK)
      {
        RequireNotNull(_moduleState.SensitivityDesign);

        var ranking = new Ranking(
          rankingViewModel.From, 
          rankingViewModel.To, 
          rankingViewModel.OutputViewModels.Filter(vm => vm.IsSelected).Map(vm => vm.Name), 
          rankingViewModel.RankedParameterViewModels.Map(vm => (vm.Name, vm.Score, vm.IsSelected))
          );

        _sensitivityDesigns.SaveRanking(_moduleState.SensitivityDesign, ranking);

        _moduleState.Ranking = ranking;
      }
    }

    private void HandleUseRankedParameters()
    {
      var toUse = _moduleState.Ranking.Parameters
        .Filter(p => p.IsSelected)
        .Map(p => p.Parameter);
      
      RequireFalse(toUse.IsEmpty);

      var parameterStates = _moduleState.ParameterStates.Map(
        ps => ps.WithIsSelected(toUse.Contains(ps.Name))
        );

      _moduleState.ParameterStates = parameterStates;
    }

    private void HandleShareRankedParameters()
    {
      var toUse = _moduleState.Ranking.Parameters
        .Filter(p => p.IsSelected)
        .Map(p => p.Parameter);

      RequireFalse(toUse.IsEmpty);

      _moduleState.ParameterStates
        .Filter(ps => toUse.Contains(ps.Name))
        .ShareStates(_appState);
    }

    private void ObserveModuleStateSensitivityDesign(object _)
    {
      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        Populate();
        PlotModel.InvalidatePlot(updateData: true);
      }
    }

    private void ObserveMeasuresStateMorrisOutputMeasures(object _)
    {
      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        PopulateMeasures();
        PlotModel.InvalidatePlot(updateData: true);
      }
    }

    private void ObserveMeasuresStateSelectedOutputName(object _)
    {
      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        PopulateMeasures();
        PlotModel.InvalidatePlot(updateData: true);
      }
    }

    private void ObserveModuleStateRanking(object _)
    {
      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        PopulateRanking();

        PopulateAnnotation();
        PlotModel.InvalidatePlot(updateData: true);
      }
    }

    private void ObserveIsVisible(object _)
    {
      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        Populate();
        PlotModel.InvalidatePlot(updateData: true);
      }
    }

    private void ObserveSelectedOutputName(object _)
    {
      if (!_reactiveSafeInvoke.React) return;

      var outputName = OutputNames[SelectedOutputName];
      _moduleState.MeasuresState.SelectedOutputName = outputName;
    }

    private void ObserveMorrisMeasureType(object _)
    {
      if (!_reactiveSafeInvoke.React) return;

      SetVerticalAxisTitle();
      PopulateSeries();
      PlotModel.InvalidatePlot(updateData: true);
    }

    private void ObserveXBeginText(object _)
    {
      PopulateAnnotation();
      PlotModel.InvalidatePlot(updateData: false);
    }

    private void ObserveXEndText(object _)
    {
      PopulateAnnotation();
      PlotModel.InvalidatePlot(updateData: false);
    }

    private void ObserveAppSettingsPropertyChange(string? propertyName)
    {
      if (!propertyName.IsThemeProperty()) return;

      PlotModel.ApplyThemeToPlotModelAndAxes();
      PlotModel.InvalidatePlot(updateData: false);
    }

    private void HandlePlotModelMouseDown(object? sender, OxyMouseDownEventArgs e)
    {
      if (!IsReady) return;

      if (e.ChangedButton != OxyMouseButton.Left) return;

      var onXScale = _annotation.InverseTransform(e.Position).X;

      if (onXScale < _xMinimum || onXScale > _xMaximum) return;

      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        XBeginText = onXScale.ToSigFigs(3).ToString(InvariantCulture);
        XEndText = onXScale.ToSigFigs(3).ToString(InvariantCulture);
      }

      PopulateAnnotation();

      _annotation.Tag = onXScale;

      PlotModel.InvalidatePlot(updateData: false);

      e.Handled = true;
    }

    private void HandlePlotModelMouseMove(object? sender, OxyMouseEventArgs e)
    {
      if (_annotation.Tag == default) return;

      var onXScale = _annotation.InverseTransform(e.Position).X;

      var start = RequireInstanceOf<double>(_annotation.Tag);
      var minimum = Min(onXScale, start);
      var maximum = Max(onXScale, start);

      if (minimum < _xMinimum) minimum = _xMinimum.Value;
      if (maximum > _xMaximum) maximum = _xMaximum.Value;

      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        XBeginText = minimum.ToSigFigs(3).ToString(InvariantCulture);
        XEndText = maximum.ToSigFigs(3).ToString(InvariantCulture);
      }

      PopulateAnnotation();

      PlotModel.InvalidatePlot(updateData: false);

      e.Handled = true;
    }

    private void HandlePlotModelMouseUp(object? sender, OxyMouseEventArgs e)
    {
      _annotation.Tag = default;
    }

    private void Unload()
    {
      PlotModel.Series.Clear();
      OutputNames = default;
      SelectedOutputName = NOT_FOUND;
      MorrisMeasureType = MorrisMeasureType.None;
      XBeginText = default;
      XEndText = default;
      RankedParameterViewModels = default;
      RankedUsing = default;
      RankedFrom = default;
      RankedTo = default;
      _xMinimum = default;
      _xMaximum = default;
      IsReady = false;
    }

    private void Populate()
    {
      if (!IsVisible)
      {
        Unload();
        return;
      }

      PopulateMeasures();
      PopulateRanking();
      PopulateAnnotation();
    }

    private void PopulateMeasures()
    {
      IsReady =
        _moduleState.SensitivityDesign != default &&
        _moduleState.MeasuresState.SelectedOutputName != default &&
        _moduleState.MeasuresState.MorrisOutputMeasures.ContainsKey(
          _moduleState.MeasuresState.SelectedOutputName
        )
        &&
        _moduleState.Trace != default;

      if (!IsReady) return;

      var isInitializing = PlotModel.Series.Count == 0;

      if (isInitializing)
      {
        RequireNotNull(_moduleState.SensitivityDesign);
        RequireNotNull(_moduleState.Trace);

        var factors = _moduleState.SensitivityDesign.DesignParameters
          .Filter(dp => dp.Distribution.DistributionType != DistributionType.Invariant)
          .Map(dp => dp.Name);

        foreach (var factor in factors)
        {
          PlotModel.Series.Add(
            new LineSeries
            {
              Title = factor,
              StrokeThickness = 2,
              Tag = factor
            });
        }

        OutputNames = _moduleState.Trace.ColumnNames
          .Skip(1)
          .OrderBy(cn => cn.ToUpperInvariant())
          .ToArr();
        SelectedOutputName = OutputNames.IndexOf(_moduleState.MeasuresState.SelectedOutputName!);
        MorrisMeasureType = MorrisMeasureType.MuStar;
        IsSelected = true;
      }

      var (mu, _, _) = _moduleState.MeasuresState.MorrisOutputMeasures[
        _moduleState.MeasuresState.SelectedOutputName!
        ];

      var x = Range(0, mu.Rows.Count)
        .Map(i => mu.Rows[i].Field<double>(0))
        .ToArray();
      _xMinimum = x.Min();
      _xMaximum = x.Max();

      SetVerticalAxisTitle();
      PopulateSeries();
    }

    private void PopulateAnnotation()
    {
      var minimum = XBegin ?? 0d;
      var maximum = XEnd ?? 0d;

      if (minimum >= maximum)
      {
        minimum = maximum = 0d;
      }

      _annotation.MinimumX = minimum;
      _annotation.MaximumX = maximum;
      _annotation.Text = maximum > minimum
        ? $"Ranking range: {minimum:G3} - {maximum:G3}"
        : default;
    }

    private void PopulateRanking()
    {
      XBeginText = _moduleState.Ranking.XBegin?.ToString(InvariantCulture);
      XEndText = _moduleState.Ranking.XEnd?.ToString(InvariantCulture);

      RankedParameterViewModels = _moduleState.Ranking.Parameters.Map<IRankedParameterViewModel>(
        p => new RankedParameterViewModel(p.Parameter, p.Score) { IsSelected = p.IsSelected }
        );
      RankedUsing = _moduleState.Ranking.Outputs;
      RankedFrom = _moduleState.Ranking.XBegin;
      RankedTo = _moduleState.Ranking.XEnd;
    }

    private void SetVerticalAxisTitle()
    {
      RequireFalse(MorrisMeasureType == MorrisMeasureType.None);

      PlotModel.GetAxis(AxisPosition.Left).AssertNotNull().Title = _morrisMeasureNames[MorrisMeasureType];
    }

    private void PopulateSeries()
    {
      RequireNotNullEmptyWhiteSpace(_moduleState.MeasuresState.SelectedOutputName);
      RequireTrue(
        _moduleState.MeasuresState.MorrisOutputMeasures.ContainsKey(
          _moduleState.MeasuresState.SelectedOutputName
          )
        );

      var (mu, muStar, sigma) = _moduleState.MeasuresState.MorrisOutputMeasures[
        _moduleState.MeasuresState.SelectedOutputName
        ];

      var seriesSource = MorrisMeasureType switch
      {
        MorrisMeasureType.Mu => mu,
        MorrisMeasureType.MuStar => muStar,
        MorrisMeasureType.Sigma => sigma,
        _ => throw new InvalidOperationException(nameof(MorrisMeasureType)),
      };

      foreach (LineSeries lineSeries in PlotModel.Series)
      {
        lineSeries.Points.Clear();

        var factor = (string)lineSeries.Tag;

        for (var row = 0; row < seriesSource.Rows.Count; ++row)
        {
          var dataRow = seriesSource.Rows[row];
          var x = dataRow.Field<double>(0);
          var y = dataRow.Field<double>(factor);
          lineSeries.Points.Add(new DataPoint(x, y));
        }
      }
    }

    private static readonly IDictionary<MorrisMeasureType, string> _morrisMeasureNames =
      new SortedDictionary<MorrisMeasureType, string>
      {
        [MorrisMeasureType.Mu] = "μ",
        [MorrisMeasureType.MuStar] = "μ*",
        [MorrisMeasureType.Sigma] = "σ"
      };

    private readonly IAppState _appState;
    private readonly IAppService _appService;
    private readonly IAppSettings _appSettings;
    private readonly ModuleState _moduleState;
    private readonly SensitivityDesigns _sensitivityDesigns;
    private readonly Simulation _simulation;
    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
    private readonly IDisposable _subscriptions;
    private readonly RectangleAnnotation _annotation;
    private double? _xMinimum;
    private double? _xMaximum;
    private bool _disposed = false;
  }
}
